import { useEffect, useState } from 'react';
import { Html5QrcodeScanner, Html5QrcodeSupportedFormats } from 'html5-qrcode';
import api from '../../api/axios';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { ArrowLeft, Check, AlertCircle } from 'lucide-react';

export const ScanPage = () => {
    const [scanResult, setScanResult] = useState(null); // { success: boolean, message: string }
    const [scanning, setScanning] = useState(true);

    const navigate = useNavigate();
    const { user } = useAuth();



    // Using a ref to hold the scanner instance if needed to clear it manually, 
    // but the library handles clearing usually via 'clear()' method.
    useEffect(() => {


        const scannerId = "reader";
        let html5QrcodeScanner;

        const onScanSuccess = async (decodedText, decodedResult) => {
            if (!decodedText) return;

            // Stop scanning immediately
            html5QrcodeScanner.clear();


            setScanning(false);

            try {
                // Parse the QR payload
                let payload;
                try {
                    payload = JSON.parse(decodedText);
                } catch (parseErr) {
                    console.error("QR Parse Error - Raw data:", decodedText);
                    setScanResult({ success: false, message: 'QR code data is not valid JSON' });
                    return;
                }



                // Validate required fields exist
                if (!payload.sessionId || !payload.token) {
                    console.error("QR Missing Fields - Payload:", payload);
                    setScanResult({ success: false, message: 'QR code missing sessionId or token' });
                    return;
                }

                console.log("Sending scan request:", { sessionId: payload.sessionId, token: payload.token?.substring(0, 50) + '...' });

                // --- LOCATION REMOVED ---
                // Directly send to API without location
                try {
                    await api.post('/Attendance/scan', {
                        sessionId: payload.sessionId,
                        token: payload.token,
                        deviceMetadata: navigator.userAgent,
                        latitude: null,
                        longitude: null
                    });

                    setScanResult({ success: true, message: 'Attendance Marked Successfully!' });

                    // Auto redirect after success
                    setTimeout(() => navigate('/student'), 3000);
                } catch (err) {
                    console.error("Scan API Error:", err);
                    const msg = err.response?.data?.message || err.response?.data || 'Invalid QR Code or Scan Failed';
                    setScanResult({ success: false, message: typeof msg === 'string' ? msg : JSON.stringify(msg) });
                }
            } catch (err) {
                console.error("Scan API Error:", err);
                const msg = err.response?.data?.message || err.response?.data || 'Invalid QR Code or Scan Failed';
                setScanResult({ success: false, message: typeof msg === 'string' ? msg : JSON.stringify(msg) });
            }
        };

        const onScanFailure = (error) => {
            // console.warn(`Code scan error = ${error}`);
        };

        if (scanning) {
            html5QrcodeScanner = new Html5QrcodeScanner(
                scannerId,
                {
                    fps: 10,
                    qrbox: { width: 250, height: 250 },
                    formatsToSupport: [Html5QrcodeSupportedFormats.QR_CODE],
                    videoConstraints: {
                        facingMode: { exact: "environment" }
                    }
                },
                false
            );
            html5QrcodeScanner.render(onScanSuccess, onScanFailure);
        }

        return () => {
            if (html5QrcodeScanner) {
                html5QrcodeScanner.clear().catch(error => {
                    console.error("Failed to clear html5QrcodeScanner. ", error);
                });
            }
        };
    }, [scanning, navigate]);
    return (
        <div className="min-h-screen bg-black flex flex-col items-center justify-center p-4 relative">
            <button
                onClick={() => navigate('/student')}
                className="absolute top-4 left-4 z-10 bg-white rounded-full p-2"
            >
                <ArrowLeft className="h-6 w-6" />
            </button>
            {scanning ? (
                <div className="w-full max-w-md bg-white rounded-3xl overflow-hidden shadow-2xl">
                    <div className="p-4 bg-gray-900 text-white text-center font-medium">
                        Scan Class QR Code
                    </div>
                    <div id="reader" className="w-full"></div>
                    <div className="p-6 text-center text-gray-500 text-sm">
                        Align the QR code within the frame to mark your attendance.
                        <div id="debug-loc" className="mt-2 text-xs text-gray-400" />
                    </div>
                </div>
            ) : (
                <div className="w-full max-w-md bg-white rounded-3xl p-8 flex flex-col items-center text-center shadow-2xl animate-fade-in-up">
                    {scanResult?.success ? (
                        <>
                            <div className="h-24 w-24 bg-green-100 rounded-full flex items-center justify-center mb-6">
                                <Check className="h-12 w-12 text-green-600" />
                            </div>
                            <h2 className="text-2xl font-bold text-gray-900 mb-2">You're In!</h2>
                            <p className="text-gray-500">{scanResult.message}</p>
                            <p className="text-sm text-gray-400 mt-4">Redirecting...</p>
                        </>
                    ) : (
                        <>
                            <div className="h-24 w-24 bg-red-100 rounded-full flex items-center justify-center mb-6">
                                <AlertCircle className="h-12 w-12 text-red-600" />

                            </div>
                            <h2 className="text-2xl font-bold text-gray-900 mb-2">Scan Failed</h2>
                            <p className="text-red-500 font-medium">{scanResult?.message}</p>



                            <button
                                onClick={() => setScanning(true)}
                                className="mt-4 w-full py-3 bg-indigo-600 text-white rounded-xl font-medium"
                            >
                                Scan Again
                            </button>
                        </>
                    )}
                </div>
            )}
        </div>
    );
};
