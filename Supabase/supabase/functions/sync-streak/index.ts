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
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? ""
    );

    const { device_uuid, current_streak, longest_streak } = await req.json();

    const rateLimited = await checkRateLimit(supabase, device_uuid || "", "sync-streak");
    if (rateLimited) return rateLimited;

    if (!device_uuid) {
      return new Response(
        JSON.stringify({ error: "device_uuid required" }),
        { status: 400, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    const { error } = await supabase
      .from("players")
      .update({
        current_streak: current_streak ?? 0,
        longest_streak: longest_streak ?? 0,
      })
      .eq("device_uuid", device_uuid);

    if (error) {
      return new Response(
        JSON.stringify({ error: error.message }),
        { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
      );
    }

    return new Response(
      JSON.stringify({ success: true }),
      { status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  } catch (err) {
    return new Response(
      JSON.stringify({ error: err.message }),
      { status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" } }
    );
  }
});
