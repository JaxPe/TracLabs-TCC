// src/TracLabs.Frontend/wwwroot/js/pdfViewer.js

/**
 * TracLabs Metrology PDF Interop Bridge
 * Provides base64 byte streaming to browser Blob URLs for viewing and downloading PDFs.
 */

(function () {
    /**
     * Converts a base64 string to a Blob object using chunked decoding.
     * Avoids call stack size limits on large files.
     * @param {string} base64Data
     * @param {string} contentType
     * @returns {Blob}
     */
    function base64ToBlob(base64Data, contentType) {
        contentType = contentType || 'application/pdf';
        var byteCharacters = atob(base64Data);
        var sliceSize = 1024;
        var byteArrays = [];

        for (var offset = 0; offset < byteCharacters.length; offset += sliceSize) {
            var slice = byteCharacters.slice(offset, offset + sliceSize);
            var byteNumbers = new Array(slice.length);
            for (var i = 0; i < slice.length; i++) {
                byteNumbers[i] = slice.charCodeAt(i);
            }
            byteArrays.push(new Uint8Array(byteNumbers));
        }

        return new Blob(byteArrays, { type: contentType });
    }

    /**
     * Triggers a browser file download from base64 content.
     * @param {string} fileName 
     * @param {string} base64Data 
     * @param {string} [contentType='application/pdf']
     */
    function downloadFileFromBase64(fileName, base64Data, contentType) {
        try {
            var blob = base64ToBlob(base64Data, contentType);
            var blobUrl = URL.createObjectURL(blob);

            var link = document.createElement('a');
            link.href = blobUrl;
            link.download = fileName || 'certificado-metrologico.pdf';
            link.style.display = 'none';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            // Revoke after download is triggered
            setTimeout(function () {
                URL.revokeObjectURL(blobUrl);
            }, 2000);
        } catch (error) {
            console.error('TracLabs PDF download error:', error);
            throw error;
        }
    }

    /**
     * Opens a base64 PDF in a new browser tab/window for real-time preview.
     * @param {string} base64Data 
     * @param {string} [contentType='application/pdf']
     */
    function viewPdfFromBase64(base64Data, contentType) {
        try {
            var blob = base64ToBlob(base64Data, contentType);
            var blobUrl = URL.createObjectURL(blob);
            window.open(blobUrl, '_blank');

            // Revoke after a delay allowing tab to finish rendering
            setTimeout(function () {
                URL.revokeObjectURL(blobUrl);
            }, 60000);
        } catch (error) {
            console.error('TracLabs PDF viewer error:', error);
            throw error;
        }
    }

    // Expose functions globally for direct IJSRuntime.InvokeVoidAsync calls
    window.downloadFileFromBase64 = downloadFileFromBase64;
    window.viewPdfFromBase64 = viewPdfFromBase64;

    // Expose under traclabsPdf namespace as well
    window.traclabsPdf = {
        downloadFileFromBase64: downloadFileFromBase64,
        viewPdfFromBase64: viewPdfFromBase64,
        base64ToBlob: base64ToBlob
    };
})();
