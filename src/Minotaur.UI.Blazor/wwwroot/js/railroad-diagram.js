// Railroad Diagram Export Functions

/**
 * Download a file with the specified content
 * @param {string} filename - Name of the file to download
 * @param {string} contentType - MIME type of the content
 * @param {string} content - Content to download
 */
window.downloadFile = function (filename, contentType, content) {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

/**
 * Export SVG element to PNG
 * @param {string} svgElementId - ID of the SVG element
 * @param {string} filename - Name of the file to download
 */
window.exportSvgToPng = function (svgElementId, filename) {
    const svgElement = document.getElementById(svgElementId);
    if (!svgElement) {
        console.error('SVG element not found:', svgElementId);
        return;
    }

    // Get SVG dimensions
    const svgRect = svgElement.getBoundingClientRect();
    const width = svgRect.width;
    const height = svgRect.height;

    // Serialize SVG to string
    const serializer = new XMLSerializer();
    let svgString = serializer.serializeToString(svgElement);

    // Add XML namespace if not present
    if (!svgString.match(/^<svg[^>]+xmlns="http:\/\/www\.w3\.org\/2000\/svg"/)) {
        svgString = svgString.replace(/^<svg/, '<svg xmlns="http://www.w3.org/2000/svg"');
    }

    // Create a canvas element
    const canvas = document.createElement('canvas');
    canvas.width = width * 2; // 2x for better quality
    canvas.height = height * 2;
    const ctx = canvas.getContext('2d');

    // Scale for better quality
    ctx.scale(2, 2);

    // Create an image from SVG
    const img = new Image();
    const svgBlob = new Blob([svgString], { type: 'image/svg+xml;charset=utf-8' });
    const url = URL.createObjectURL(svgBlob);

    img.onload = function () {
        // Draw white background
        ctx.fillStyle = '#f8f9fa';
        ctx.fillRect(0, 0, width, height);

        // Draw the image
        ctx.drawImage(img, 0, 0, width, height);

        // Convert canvas to PNG blob
        canvas.toBlob(function (blob) {
            const pngUrl = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = pngUrl;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(pngUrl);
            URL.revokeObjectURL(url);
        }, 'image/png');
    };

    img.onerror = function (error) {
        console.error('Error loading SVG image:', error);
        URL.revokeObjectURL(url);
    };

    img.src = url;
};
