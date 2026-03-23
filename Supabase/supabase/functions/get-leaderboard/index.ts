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
    const game_date = url.searchParams.get("game_date");
    const device_uuid = url.searchParams.get("device_uuid") || "";
    const limit = parseInt(url.searchParams.get("limit") || "100");
    const offset = parseInt(url.searchParams.get("offset") || "0");

    if (!game_date) {
      return new Response(
        JSON.stringify({ error: "game_date parameter required" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    const { data, error } = await supabase
      .from("daily_scores")
      .select(
        `
        device_uuid,
        score,
        largest_merges,
        players!inner(display_name)
      `
      )
      .eq("game_date", game_date)
      .order("score", { ascending: false })
      .range(offset, offset + limit - 1);

    if (error) {
      return new Response(
        JSON.stringify({ error: error.message }),
        { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    // Flatten with dense ranking — tied scores get the same rank
    let currentRank = offset + 1;
    let previousScore = -1;
    const entries = (data || []).map((row: any, index: number) => {
      if (row.score !== previousScore) {
        currentRank = offset + index + 1;
        previousScore = row.score;
      }
      return {
        device_uuid: row.device_uuid,
        display_name: row.players?.display_name || "Player",
        score: row.score,
        largest_merges: row.largest_merges || [],
        rank: currentRank,
        is_current_player: row.device_uuid === device_uuid,
      };
    });

    return new Response(JSON.stringify(entries), {
      status: 200,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  } catch (err) {
    return new Response(
      JSON.stringify({ error: err.message }),
      { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  }
});
