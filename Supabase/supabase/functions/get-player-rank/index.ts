import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { corsHeaders, verifyApiKey } from "../_shared/auth.ts";

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  const authError = verifyApiKey(req);
  if (authError) return authError;

  try {
    const supabase = createClient(
      Deno.env.get("SUPABASE_URL") ?? "",
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? ""
    );

    const url = new URL(req.url);
    const device_uuid = url.searchParams.get("device_uuid");
    const game_date = url.searchParams.get("game_date");

    if (!device_uuid || !game_date) {
      return new Response(
        JSON.stringify({ error: "device_uuid and game_date parameters required" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Single query: get rank + total using window functions
    const { data, error } = await supabase.rpc("get_player_rank", {
      p_device_uuid: device_uuid,
      p_game_date: game_date,
    });

    if (error) {
      // Fallback if RPC doesn't exist yet — use old method
      return await fallbackRank(supabase, device_uuid, game_date);
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
async function fallbackRank(supabase: any, device_uuid: string, game_date: string) {
  const { data: playerScore } = await supabase
    .from("daily_scores")
    .select("score")
    .eq("device_uuid", device_uuid)
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
