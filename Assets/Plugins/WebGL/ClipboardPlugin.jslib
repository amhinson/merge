mergeInto(LibraryManager.library, {
    CopyImageToClipboardJS: function(base64PngPtr) {
        var base64Png = UTF8ToString(base64PngPtr);
        var byteCharacters = atob(base64Png);
        var byteNumbers = new Array(byteCharacters.length);
        for (var i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        var byteArray = new Uint8Array(byteNumbers);
        var blob = new Blob([byteArray], { type: 'image/png' });

        if (navigator.clipboard && navigator.clipboard.write) {
            var item = new ClipboardItem({ 'image/png': blob });
            navigator.clipboard.write([item]).then(function() {
                console.log('Image copied to clipboard');
            }).catch(function(err) {
                console.error('Failed to copy image:', err);
            });
        } else {
            console.warn('Clipboard API not available');
        }
    }
});
