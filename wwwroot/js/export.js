window.blazePortExport = {
    downloadPdf: function (fileName, base64Data) {
        if (!fileName || !base64Data) {
            return;
        }

        try {
            const binaryString = atob(base64Data);
            const len = binaryString.length;
            const bytes = new Uint8Array(len);
            for (let i = 0; i < len; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }

            const blob = new Blob([bytes], { type: "application/pdf" });
            const url = URL.createObjectURL(blob);

            // Download the PDF file
            const a = document.createElement("a");
            a.href = url;
            a.download = fileName;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);

            setTimeout(() => URL.revokeObjectURL(url), 60_000);
        } catch (e) {
            console.error("PDF download failed", e);
        }
    }
};

