var WebAuthPlugin = {
    /**
     * Opens the Supabase OAuth flow in the same window.
     * After sign-in, Supabase redirects back with tokens in the URL fragment.
     */
    WebAuth_StartOAuthSignIn: function(supabaseUrlPtr, supabaseKeyPtr, redirectUrlPtr, providerPtr) {
        var supabaseUrl = UTF8ToString(supabaseUrlPtr);
        var supabaseKey = UTF8ToString(supabaseKeyPtr);
        var redirectUrl = UTF8ToString(redirectUrlPtr);
        var provider = UTF8ToString(providerPtr);

        var authUrl = supabaseUrl + "/auth/v1/authorize"
            + "?provider=" + encodeURIComponent(provider)
            + "&redirect_to=" + encodeURIComponent(redirectUrl)
            + "&apikey=" + encodeURIComponent(supabaseKey);

        window.location.href = authUrl;
    },

    /**
     * Returns true if the browser is on an Apple platform (macOS, iOS, iPadOS).
     */
    WebAuth_IsAppleDevice: function() {
        var ua = navigator.userAgent || "";
        var platform = navigator.platform || "";
        // Check for iOS/iPadOS/macOS
        if (/iPhone|iPad|iPod/.test(ua)) return true;
        if (/Macintosh|MacIntel|MacPPC|Mac68K/.test(platform)) return true;
        // iPadOS 13+ reports as Mac with touch support
        if (/Mac/.test(platform) && navigator.maxTouchPoints > 1) return true;
        return false;
    },

    /**
     * Checks the URL fragment for Supabase auth tokens (present after OAuth redirect).
     * Returns a JSON string with the tokens, or empty string if none found.
     */
    WebAuth_GetTokensFromUrl: function() {
        var hash = window.location.hash;
        if (!hash || hash.length < 2) {
            var bufSize = 1;
            var buf = _malloc(bufSize);
            stringToUTF8("", buf, bufSize);
            return buf;
        }

        // Parse fragment: #access_token=...&refresh_token=...&expires_in=...&token_type=bearer
        var params = {};
        var pairs = hash.substring(1).split("&");
        for (var i = 0; i < pairs.length; i++) {
            var kv = pairs[i].split("=");
            if (kv.length === 2) {
                params[decodeURIComponent(kv[0])] = decodeURIComponent(kv[1]);
            }
        }

        var result = "";
        if (params["access_token"]) {
            result = JSON.stringify({
                access_token: params["access_token"] || "",
                refresh_token: params["refresh_token"] || "",
                expires_in: parseInt(params["expires_in"] || "3600", 10),
                token_type: params["token_type"] || "bearer"
            });

            // Clean the URL fragment so tokens aren't visible/bookmarkable
            if (window.history && window.history.replaceState) {
                window.history.replaceState(null, "", window.location.pathname + window.location.search);
            }
        }

        var bufSize = lengthBytesUTF8(result) + 1;
        var buf = _malloc(bufSize);
        stringToUTF8(result, buf, bufSize);
        return buf;
    }
};

mergeInto(LibraryManager.library, WebAuthPlugin);
