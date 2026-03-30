#!/usr/bin/env ruby
# Generates the Apple Sign In client secret JWT for Supabase.
# Usage: ruby generate-apple-secret.rb
#
# Requires: gem install jwt

require "jwt"
require "time"

# ─── Fill these in ───
TEAM_ID    = "9N2D7WP5M2"         # Apple Developer Team ID
CLIENT_ID  = "com.murge.game.signin"  # Your Service ID
KEY_ID     = "5R6DL28P6Q"         # From the .p8 filename / key detail page
KEY_FILE   = File.expand_path("../../AuthKey_5R6DL28P6Q.p8", __FILE__)

# ─── Generate ───
key = OpenSSL::PKey::EC.new(File.read(KEY_FILE))

now = Time.now.to_i
payload = {
  iss: TEAM_ID,
  iat: now,
  exp: now + (86400 * 180), # 180 days (max allowed by Apple)
  aud: "https://appleid.apple.com",
  sub: CLIENT_ID,
}

header = {
  kid: KEY_ID,
  alg: "ES256",
}

token = JWT.encode(payload, key, "ES256", header)
puts "\n✅ Apple client secret JWT (valid for 180 days):\n\n"
puts token
puts "\n\nPaste this into Supabase > Authentication > Providers > Apple > Secret Key\n"
