import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { corsHeaders, getAuthenticatedUserId } from "../_shared/auth.ts";

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const url = new URL(req.url);
    // Support both GET (query params) and POST (body)
    const body = req.method === "POST" ? await req.json().catch(() => ({})) : {};
    const queryBody = {
      ...body,
      user_id: body.user_id || url.searchParams.get("user_id"),
      device_uuid: body.device_uuid || url.searchParams.get("device_uuid"),
    };

    const { userId, error: authError } = await getAuthenticatedUserId(req, queryBody);
    if (authError) return authError;

    const supabase = createClient(
      Deno.env.get("SUPABASE_URL") ?? "",
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? ""
    );

    // Get player info
    const { data: player, error: playerError } = await supabase
      .from("players")
      .select("*")
      .eq("user_id", userId)
      .single();

    if (playerError || !player) {
      return new Response(
        JSON.stringify({ error: "Player not found" }),
        { status: 404, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Get today's score if a game_date was provided (body already parsed above)
    const game_date = queryBody.game_date || url.searchParams.get("game_date");

    let today_score = 0;
    let day_number = 0;
    let merge_counts: number[] = [];

    if (game_date) {
      const { data: todayData } = await supabase
        .from("daily_scores")
        .select("score, day_number, merge_counts")
        .eq("user_id", userId)
        .eq("game_date", game_date)
        .single();

      if (todayData) {
        today_score = todayData.score || 0;
        day_number = todayData.day_number || 0;
        merge_counts = todayData.merge_counts || [];
      }
    }

    return new Response(
      JSON.stringify({
        display_name: player.display_name,
        current_streak: player.current_streak || 0,
        longest_streak: player.longest_streak || 0,
        today_score,
        day_number,
        merge_counts,
      }),
      { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  } catch (err) {
    return new Response(
      JSON.stringify({ error: err.message }),
      { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  }
});
