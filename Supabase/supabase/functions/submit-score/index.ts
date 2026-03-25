import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { corsHeaders, verifyApiKey } from "../_shared/auth.ts";
import { checkRateLimit } from "../_shared/rate-limit.ts";

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  const authError = verifyApiKey(req);
  if (authError) return authError;

  try {
    const supabase = createClient(
      Deno.env.get("SUPABASE_URL") ?? "",
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
    );

    const {
      device_uuid,
      display_name,
      score,
      game_date,
      day_number,
      merge_counts,
      longest_chain,
    } = await req.json();

    if (!device_uuid || !game_date || score == null) {
      return new Response(
        JSON.stringify({ error: "Missing required fields" }),
        {
          status: 400,
          headers: { ...corsHeaders, "Content-Type": "application/json" },
        },
      );
    }

    // Rate limit
    const rateLimited = await checkRateLimit(supabase, device_uuid, "submit-score");
    if (rateLimited) return rateLimited;

    // --- Score validation ---
    // Hard cap: no game should exceed 99,999 points
    const MAX_SCORE = 99999;
    if (
      typeof score !== "number" ||
      score < 0 ||
      score > MAX_SCORE ||
      !Number.isInteger(score)
    ) {
      return new Response(JSON.stringify({ error: "Invalid score" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // Validate longest chain
    if (
      typeof longest_chain === "number" &&
      (longest_chain < 0 || longest_chain > 99)
    ) {
      return new Response(JSON.stringify({ error: "Invalid chain value" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // Validate game_date is within ±1 day of server time
    // This allows all timezones (UTC-12 to UTC+14) without requiring UTC on client
    const submitted = new Date(game_date + "T00:00:00Z");
    const now = new Date();
    const diffMs = Math.abs(now.getTime() - submitted.getTime());
    const diffDays = diffMs / (1000 * 60 * 60 * 24);
    if (isNaN(submitted.getTime()) || diffDays > 1.5) {
      return new Response(
        JSON.stringify({ error: "Invalid game date" }),
        {
          status: 400,
          headers: { ...corsHeaders, "Content-Type": "application/json" },
        },
      );
    }

    const { error: playerError } = await supabase
      .from("players")
      .upsert(
        { device_uuid, display_name: display_name || "Player" },
        { onConflict: "device_uuid" },
      );

    if (playerError) {
      return new Response(
        JSON.stringify({
          error: "Failed to upsert player",
          details: playerError.message,
        }),
        {
          status: 500,
          headers: { ...corsHeaders, "Content-Type": "application/json" },
        },
      );
    }

    const { data: existing } = await supabase
      .from("daily_scores")
      .select("id")
      .eq("device_uuid", device_uuid)
      .eq("game_date", game_date)
      .single();

    if (existing) {
      return new Response(
        JSON.stringify({ error: "Score already submitted for this date" }),
        {
          status: 409,
          headers: { ...corsHeaders, "Content-Type": "application/json" },
        },
      );
    }

    const { error: scoreError } = await supabase.from("daily_scores").insert({
      device_uuid,
      game_date,
      score,
      day_number: day_number || 1,
      merge_counts: merge_counts || [],
      longest_chain: longest_chain || 0,
    });

    if (scoreError) {
      return new Response(
        JSON.stringify({
          error: "Failed to insert score",
          details: scoreError.message,
        }),
        {
          status: 500,
          headers: { ...corsHeaders, "Content-Type": "application/json" },
        },
      );
    }

    return new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  } catch (err) {
    return new Response(JSON.stringify({ error: err.message }), {
      status: 500,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  }
});
