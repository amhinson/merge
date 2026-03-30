import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { corsHeaders, getAuthenticatedUserId } from "../_shared/auth.ts";

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const url = new URL(req.url);
    const queryBody = {
      user_id: url.searchParams.get("user_id"),
      device_uuid: url.searchParams.get("device_uuid"),
    };

    const { userId, error: authError } = await getAuthenticatedUserId(req, queryBody);
    if (authError) return authError;

    const supabase = createClient(
      Deno.env.get("SUPABASE_URL") ?? "",
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? ""
    );

    const game_date = url.searchParams.get("game_date");

    if (!game_date) {
      return new Response(
        JSON.stringify({ error: "game_date parameter required" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Single query: get rank + total using window functions
    const { data, error } = await supabase.rpc("get_player_rank", {
      p_user_id: userId,
      p_game_date: game_date,
    });

    if (error) {
      // Fallback if RPC doesn't exist yet — use old method
      return await fallbackRank(supabase, userId!, game_date);
    }

    if (!data || data.length === 0) {
      return new Response(
        JSON.stringify({ rank: -1, total_players: 0, percentile: "" }),
        { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    const { rank, total_players } = data[0];
    const percentileNum = Math.ceil((rank / total_players) * 100);

    return new Response(
      JSON.stringify({ rank, total_players, percentile: `Top ${percentileNum}%` }),
      { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  } catch (err) {
    return new Response(
      JSON.stringify({ error: err.message }),
      { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  }
});

// Fallback using 3 queries (works without the DB function)
async function fallbackRank(supabase: any, user_id: string, game_date: string) {
  const { data: playerScore } = await supabase
    .from("daily_scores")
    .select("score")
    .eq("user_id", user_id)
    .eq("game_date", game_date)
    .single();

  if (!playerScore) {
    return new Response(
      JSON.stringify({ rank: -1, total_players: 0, percentile: "" }),
      { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  }

  const { count: higherCount } = await supabase
    .from("daily_scores")
    .select("id", { count: "exact", head: true })
    .eq("game_date", game_date)
    .gt("score", playerScore.score);

  const { count: totalPlayers } = await supabase
    .from("daily_scores")
    .select("id", { count: "exact", head: true })
    .eq("game_date", game_date);

  const rank = (higherCount || 0) + 1;
  const total = totalPlayers || 1;
  const percentileNum = Math.ceil((rank / total) * 100);

  return new Response(
    JSON.stringify({ rank, total_players: total, percentile: `Top ${percentileNum}%` }),
    { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
  );
}
