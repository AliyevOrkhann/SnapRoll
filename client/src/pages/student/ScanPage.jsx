import { useEffect, useState, useRef } from 'react';
import { Html5Qrcode, Html5QrcodeSupportedFormats } from 'html5-qrcode';
import api from '../../api/axios';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { ArrowLeft, Check, AlertCircle, Smartphone, RefreshCw } from 'lucide-react';

export const ScanPage = () => {
    const [scanResult, setScanResult] = useState(null); // { success: boolean, message: string }
    const [scanning, setScanning] = useState(true);
    const [zoom, setZoom] = useState(1);
    const [zoomCapabilities, setZoomCapabilities] = useState(null); // { min, max, step }
    const [cameraError, setCameraError] = useState(null);

    const navigate = useNavigate();
    const { user } = useAuth();
    const html5QrcodeRef = useRef(null);
    const scannerId = "reader-custom";

    // Ref to track applying zoom to prevent stutter
    const applyingZoom = useRef(false);

    // Initialize Scanner
    useEffect(() => {
        if (!scanning) return;

        const startScanner = async () => {
            try {
                if (!html5QrcodeRef.current) {
                    html5QrcodeRef.current = new Html5Qrcode(scannerId);
                }

                const config = {
                    fps: 15, // Increased FPS for smoother scanning
                    // No qrbox - we use our own custom overlay for the scan guide
                    aspectRatio: 1.0,
                    videoConstraints: {
                        facingMode: { exact: "environment" },
                        width: { ideal: 1920 },
                        height: { ideal: 1080 },
                        focusMode: "continuous" // Attempt to force continuous focus
                    }
                };

                await html5QrcodeRef.current.start(
                    { facingMode: { exact: "environment" } },
                    config,
                    onScanSuccess,
                    onScanFailure
                );

                // Check capabilities
                try {
                    const stream = document.querySelector(`#${scannerId} video`)?.srcObject;
                    if (stream) {
                        const track = stream.getVideoTracks()[0];
                        const capabilities = track.getCapabilities();
                        if (capabilities.zoom) {
                            // Ensure minimum zoom is at least 1.0 (no zoom out below 1x)
                            const minZoom = Math.max(1, capabilities.zoom.min);
                            setZoomCapabilities({
                                min: minZoom,
                                max: capabilities.zoom.max,
                                step: capabilities.zoom.step
                            });
                            const currentZoom = track.getSettings().zoom;
                            // Set initial zoom to at least 1.0
                            setZoom(currentZoom ? Math.max(1, currentZoom) : 1);
                        }
                    }
                } catch (err) {
                    console.warn("Could not get camera capabilities for zoom:", err);
                }

            } catch (err) {
                console.error("Error starting scanner:", err);
                setCameraError("Could not access camera. Please ensure permissions are granted.");
            }
        };

        const timer = setTimeout(startScanner, 100);

        return () => {
            clearTimeout(timer);
            if (html5QrcodeRef.current && floatIsScanning.current) {
                html5QrcodeRef.current.stop().then(() => html5QrcodeRef.current.clear()).catch(console.error);
            }
        };
    }, [scanning]);

    const floatIsScanning = useRef(scanning);
    useEffect(() => { floatIsScanning.current = scanning; }, [scanning]);

    // Throttled Zoom Application
    const applyZoom = async (newZoom) => {
        if (applyingZoom.current) return;
        applyingZoom.current = true;

        try {
            const stream = document.querySelector(`#${scannerId} video`)?.srcObject;
            if (stream) {
                const track = stream.getVideoTracks()[0];
                // Only apply if track is live
                if (track && track.readyState === 'live') {
                    await track.applyConstraints({ advanced: [{ zoom: newZoom }] });
                }
            }
        } catch (err) {
            console.error("Failed to apply zoom:", err);
        } finally {
            // Small delay to allow hardware to settle
            setTimeout(() => { applyingZoom.current = false; }, 200); // Increased throttle time slightly
        }
    };

    const handleZoomSlider = (e) => {
        const newZoom = parseFloat(e.target.value);
        setZoom(newZoom);
        applyZoom(newZoom);
    };



    const onScanSuccess = async (decodedText, decodedResult) => {
        if (!floatIsScanning.current) return;
        
        // Mark as not scanning immediately to prevent cleanup from double-stopping
        floatIsScanning.current = false;

        // 1. Stop the camera FIRST before unmounting the element (which happens when setScanning(false) runs)
        if (html5QrcodeRef.current) {
            try {
                if (html5QrcodeRef.current.isScanning) {
                    await html5QrcodeRef.current.stop();
                }
                html5QrcodeRef.current.clear();
            } catch (err) {
                console.error("Error stopping scanner on success", err);
            }
        }

        // 2. Set loading/processing state if you had one, or just set scanning false logic
        // But we need to ensure we don't show "Scan Failed" immediately. 
        // We will initialize scanResult to a "Processing..." state or similar if strictly null doesn't cover it.
        // Actually, let's keep it null but handle the UI to show a loader if scanning is false AND scanResult is null.

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

            try {
                await api.post('/Attendance/scan', {
                    sessionId: payload.sessionId,
                    token: payload.token,
                    deviceMetadata: navigator.userAgent,
                    latitude: null,
                    longitude: null
                });

                setScanResult({ success: true, message: 'Attendance Marked Successfully!' });
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

    return (
        <div className="fixed inset-0 bg-black flex flex-col z-50">
            {/* Header Controls */}
            <div className="absolute top-0 left-0 right-0 z-20 p-4 flex justify-between items-center bg-gradient-to-b from-black/70 to-transparent">
                <button
                    onClick={() => navigate('/student')}
                    className="bg-white/20 backdrop-blur-md rounded-full p-3 text-white hover:bg-white/30 transition"
                >
                    <ArrowLeft className="h-6 w-6" />
                </button>
                <div className="text-white font-medium text-lg drop-shadow-md">Scan Attendance</div>
                <div className="w-12"></div> {/* Spacer for center alignment */}
            </div>

            {/* Scanner Viewport */}
            {scanning ? (
                <div className="relative w-full h-full bg-black flex flex-col justify-center items-center overflow-hidden">
                    <div id={scannerId} className="w-full h-full object-cover"></div>

                    {/* Custom Overlay */}
                    <div className="absolute inset-0 pointer-events-none flex items-center justify-center">
                        <div className="w-72 h-72 border-2 border-white/50 rounded-2xl relative">
                            <div className="absolute top-0 left-0 w-6 h-6 border-t-4 border-l-4 border-indigo-500 rounded-tl-lg -mt-1 -ml-1"></div>
                            <div className="absolute top-0 right-0 w-6 h-6 border-t-4 border-r-4 border-indigo-500 rounded-tr-lg -mt-1 -mr-1"></div>
                            <div className="absolute bottom-0 left-0 w-6 h-6 border-b-4 border-l-4 border-indigo-500 rounded-bl-lg -mb-1 -ml-1"></div>
                            <div className="absolute bottom-0 right-0 w-6 h-6 border-b-4 border-r-4 border-indigo-500 rounded-br-lg -mb-1 -mr-1"></div>
                            <div className="w-full h-full bg-indigo-500/10 animate-pulse"></div>
                        </div>
                        <p className="absolute mt-80 text-white/80 text-sm font-medium bg-black/40 px-4 py-2 rounded-full backdrop-blur-sm">
                            Align QR code within frame
                        </p>
                    </div>

                    {/* Camera Error */}
                    {cameraError && (
                        <div className="absolute inset-0 bg-black flex items-center justify-center p-6 text-center">
                            <div className="text-white max-w-sm">
                                <AlertCircle className="h-12 w-12 text-red-500 mx-auto mb-4" />
                                <h3 className="text-xl font-bold mb-2">Camera Error</h3>
                                <p className="text-gray-400">{cameraError}</p>
                                <button
                                    onClick={() => navigate('/student')}
                                    className="mt-6 px-6 py-2 bg-white text-black rounded-full font-medium"
                                >
                                    Go Back
                                </button>
                            </div>
                        </div>
                    )}

                    {/* Zoom Controls */}
                    {/* Zoom Controls */}
                    {zoomCapabilities && (
                        <div className="absolute bottom-20 left-0 right-0 flex flex-col items-center justify-center p-4 z-20">
                            <div className="bg-black/50 backdrop-blur-md rounded-full px-8 py-3 flex items-center gap-4 w-full max-w-sm transition-colors hover:bg-black/60">
                                <Smartphone className="h-5 w-5 text-white/70" />
                                <div className="flex-1 flex flex-col items-center">
                                    <input
                                        type="range"
                                        min={zoomCapabilities.min}
                                        max={zoomCapabilities.max}
                                        step={zoomCapabilities.step}
                                        value={zoom}
                                        onChange={handleZoomSlider}
                                        className="w-full h-1 bg-white/30 rounded-lg appearance-none cursor-pointer accent-indigo-500"
                                    />
                                    <span className="text-xs text-white/80 font-mono mt-1">{zoom.toFixed(1)}x</span>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            ) : (
                // Success / Failure State
                <div className="w-full h-full flex items-center justify-center p-6 bg-gray-50">
                    <div className="w-full max-w-md bg-white rounded-3xl p-8 flex flex-col items-center text-center shadow-xl animate-fade-in-up">
                        {!scanResult ? (
                            // Loading state
                            <>
                                <RefreshCw className="h-12 w-12 text-indigo-600 animate-spin mb-6" />
                                <h2 className="text-2xl font-bold text-gray-900 mb-2">Processing...</h2>
                                <p className="text-gray-500">Verifying attendance...</p>
                            </>
                        ) : scanResult.success ? (
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
                                <p className="text-red-500 font-medium mb-2">{scanResult.message}</p>
                                <p className="text-sm text-gray-400">Try zooming in or getting closer.</p>

                                <div className="grid grid-cols-2 gap-3 w-full mt-6">
                                    <button
                                        onClick={() => navigate('/student')}
                                        className="py-3 bg-gray-100 text-gray-700 rounded-xl font-medium hover:bg-gray-200"
                                    >
                                        Cancel
                                    </button>
                                    <button
                                        onClick={() => {
                                            setScanResult(null);
                                            setScanning(true);
                                        }}
                                        className="py-3 bg-indigo-600 text-white rounded-xl font-medium hover:bg-indigo-700"
                                    >
                                        Scan Again
                                    </button>
                                </div>
                            </>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
};
