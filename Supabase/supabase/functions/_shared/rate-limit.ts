import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { corsHeaders } from "./auth.ts";

/**
 * Simple per-device rate limiter using a Supabase table.
 * Checks if the device has exceeded the allowed number of requests
 * within a time window. Lightweight — single query per check.
 *
 * Requires table:
 *   CREATE TABLE rate_limits (
 *     id BIGSERIAL PRIMARY KEY,
 *     device_uuid TEXT NOT NULL,
 *     function_name TEXT NOT NULL,
 *     created_at TIMESTAMPTZ DEFAULT now()
 *   );
 *   CREATE INDEX idx_rate_limits_lookup ON rate_limits (device_uuid, function_name, created_at);
 */

interface RateLimitConfig {
  maxRequests: number;    // max requests allowed
  windowSeconds: number;  // within this time window
}

const DEFAULTS: Record<string, RateLimitConfig> = {
  "submit-score":       { maxRequests: 5,  windowSeconds: 60 },
  "register-player":    { maxRequests: 10, windowSeconds: 3600 },
  "update-display-name": { maxRequests: 10, windowSeconds: 60 },
  "sync-streak":        { maxRequests: 10, windowSeconds: 60 },
};

/**
 * Check rate limit for a device + function. Returns null if allowed,
 * or a 429 Response if rate limited.
 */
export async function checkRateLimit(
  supabase: ReturnType<typeof createClient>,
  deviceUuid: string,
  functionName: string,
): Promise<Response | null> {
  const config = DEFAULTS[functionName];
  if (!config) return null; // no limit configured for this function

  const windowStart = new Date(
    Date.now() - config.windowSeconds * 1000,
  ).toISOString();

  // Opportunistic cleanup: ~1% of requests purge old entries
  if (Math.random() < 0.01) {
    await supabase.from("rate_limits").delete().lt("created_at", windowStart);
  }

  // Count recent requests
  const { count, error } = await supabase
    .from("rate_limits")
    .select("id", { count: "exact", head: true })
    .eq("device_uuid", deviceUuid)
    .eq("function_name", functionName)
    .gte("created_at", windowStart);

  if (error) {
    // Don't block on rate limit errors — fail open
    console.error("Rate limit check failed:", error.message);
    return null;
  }

  if ((count ?? 0) >= config.maxRequests) {
    return new Response(
      JSON.stringify({ error: "Too many requests, try again later" }),
      {
        status: 429,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      },
    );
  }

  // Record this request
  await supabase.from("rate_limits").insert({
    device_uuid: deviceUuid,
    function_name: functionName,
  });

  return null; // allowed
}
