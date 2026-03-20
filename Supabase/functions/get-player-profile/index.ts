import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type",
};

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const supabase = createClient(
      Deno.env.get("SUPABASE_URL") ?? "",
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? ""
    );

    const url = new URL(req.url);
    const device_uuid = url.searchParams.get("device_uuid");

    if (!device_uuid) {
      return new Response(
        JSON.stringify({ error: "device_uuid parameter required" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Get player info
    const { data: player, error: playerError } = await supabase
      .from("players")
      .select("*")
      .eq("device_uuid", device_uuid)
      .single();

    if (playerError || !player) {
      return new Response(
        JSON.stringify({ error: "Player not found" }),
        { status: 404, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Get recent scores (last 10)
    const { data: recentScores } = await supabase
      .from("daily_scores")
      .select("game_date, score, day_number, largest_merges")
      .eq("device_uuid", device_uuid)
      .order("game_date", { ascending: false })
      .limit(10);

    return new Response(
      JSON.stringify({
        display_name: player.display_name,
        current_streak: player.current_streak,
        longest_streak: player.longest_streak,
        created_at: player.created_at,
        recent_scores: recentScores || [],
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
