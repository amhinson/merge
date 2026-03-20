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

    // Get the player's score for this date
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

    // Count how many players scored higher
    const { count: higherCount } = await supabase
      .from("daily_scores")
      .select("id", { count: "exact", head: true })
      .eq("game_date", game_date)
      .gt("score", playerScore.score);

    // Count total players for this date
    const { count: totalPlayers } = await supabase
      .from("daily_scores")
      .select("id", { count: "exact", head: true })
      .eq("game_date", game_date);

    const rank = (higherCount || 0) + 1;
    const total = totalPlayers || 1;
    const percentileNum = Math.ceil((rank / total) * 100);
    const percentile = `Top ${percentileNum}%`;

    return new Response(
      JSON.stringify({ rank, total_players: total, percentile }),
      { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  } catch (err) {
    return new Response(
      JSON.stringify({ error: err.message }),
      { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  }
});
