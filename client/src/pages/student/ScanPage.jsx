import { useEffect, useState, useRef } from 'react';
import { Html5QrcodeScanner, Html5QrcodeSupportedFormats } from 'html5-qrcode';
import api from '../../api/axios';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { ArrowLeft, Check, AlertCircle, MapPin, Loader2 } from 'lucide-react';

export const ScanPage = () => {
    const [scanResult, setScanResult] = useState(null); // { success: boolean, message: string }
    const [permissionStatus, setPermissionStatus] = useState('pending'); // 'pending' | 'granted' | 'denied' | 'unsupported'
    const [cameraStatus, setCameraStatus] = useState('pending'); // 'pending' | 'granted' | 'denied' | 'error'
    const [cameraError, setCameraError] = useState(null);
    const [coords, setCoords] = useState(null); // { latitude, longitude }
    const [scanning, setScanning] = useState(true);

    const navigate = useNavigate();
    const scannerRef = useRef(null);

    // 1. Request Location on Mount
    useEffect(() => {
        if (!navigator.geolocation) {
            setPermissionStatus('unsupported');
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => {
                setCoords({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                });
                setPermissionStatus('granted');
            },
            (error) => {
                console.error("Location error:", error);
                setPermissionStatus('denied');
            },
            { enableHighAccuracy: true }
        );
    }, []);

    // 2. Request Camera Permission when location is ready
    useEffect(() => {
        if (permissionStatus !== 'granted') return;

        const requestCameraPermission = async () => {
            try {
                // Check if mediaDevices is available
                if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                    setCameraStatus('error');
                    setCameraError('Camera not supported on this device/browser');
                    return;
                }

                // Request camera permission explicitly
                const stream = await navigator.mediaDevices.getUserMedia({ 
                    video: { facingMode: 'environment' } 
                });
                
                // Permission granted - stop the stream immediately (scanner will request again)
                stream.getTracks().forEach(track => track.stop());
                setCameraStatus('granted');
            } catch (err) {
                console.error("Camera permission error:", err);
                if (err.name === 'NotAllowedError' || err.name === 'PermissionDeniedError') {
                    setCameraStatus('denied');
                    setCameraError('Camera access was denied. Please allow camera access in your browser settings.');
                } else if (err.name === 'NotFoundError' || err.name === 'DevicesNotFoundError') {
                    setCameraStatus('error');
                    setCameraError('No camera found on this device.');
                } else if (err.name === 'NotReadableError' || err.name === 'TrackStartError') {
                    setCameraStatus('error');
                    setCameraError('Camera is in use by another application.');
                } else {
                    setCameraStatus('error');
                    setCameraError(err.message || 'Failed to access camera');
                }
            }
        };

        requestCameraPermission();
    }, [permissionStatus]);

    // 3. Initialize Scanner ONLY when location AND camera are granted
    useEffect(() => {
        if (permissionStatus !== 'granted' || cameraStatus !== 'granted' || !scanning || scanResult) return;

        const scannerId = "reader";
        let scannerInst = null;
        let timerId = null;

        const onScanSuccess = async (decodedText, decodedResult) => {
            if (!decodedText) return;

            // Stop scanning logic
            if (scannerInst) {
                scannerInst.clear().catch(err => console.error("Failed to clear", err));
            }
            setScanning(false);

            try {
                // Parse Payload
                let payload;
                try {
                    payload = JSON.parse(decodedText);
                } catch (e) {
                    setScanResult({ success: false, message: 'Invalid QR Code Format' });
                    return;
                }

                if (!payload.sessionId || !payload.token) {
                    setScanResult({ success: false, message: 'Invalid QR Code Data' });
                    return;
                }

                // Send to API
                await api.post('/Attendance/scan', {
                    sessionId: payload.sessionId,
                    token: payload.token,
                    deviceMetadata: navigator.userAgent,
                    latitude: coords?.latitude || null,
                    longitude: coords?.longitude || null
                });

                setScanResult({ success: true, message: 'Attendance Marked Successfully!' });
                setTimeout(() => navigate('/student'), 3000);

            } catch (err) {
                console.error("Scan API Error:", err);
                const msg = err.response?.data?.message || 'Scan Failed';
                setScanResult({ success: false, message: typeof msg === 'string' ? msg : JSON.stringify(msg) });
            }
        };

        const onScanFailure = (error) => {
            // console.warn(error);
        };

        // Initialize with a small delay to prevent race conditions on mobile (React StrictMode)
        const initScanner = () => {
            try {
                // Double check if element exists
                const element = document.getElementById(scannerId);
                if (!element) return;
                element.innerHTML = ""; // Clear any existing scanner cleanup residue

                const scanner = new Html5QrcodeScanner(
                    scannerId,
                    {
                        fps: 10,
                        qrbox: { width: 250, height: 250 },
                        formatsToSupport: [Html5QrcodeSupportedFormats.QR_CODE],
                        videoConstraints: {
                            facingMode: "environment"
                        }
                    },
                    false
                );

                scanner.render(onScanSuccess, onScanFailure);
                scannerInst = scanner;
                scannerRef.current = scanner;
            } catch (e) {
                console.error("Scanner Initialization Error", e);
            }
        };

        timerId = setTimeout(initScanner, 500);

        return () => {
            clearTimeout(timerId);
            if (scannerInst) {
                scannerInst.clear().catch(e => console.error("Cleanup error", e));
            }
        };
    }, [permissionStatus, cameraStatus, scanning, coords, navigate, scanResult]);

    // Retry Location Handler
    const handleRetryLocation = () => {
        setPermissionStatus('pending');
        navigator.geolocation.getCurrentPosition(
            (position) => {
                setCoords({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                });
                setPermissionStatus('granted');
            },
            (error) => {
                console.error("Location error:", error);
                setPermissionStatus('denied');
            },
            { enableHighAccuracy: true }
        );
    };

    // Retry Camera Handler
    const handleRetryCamera = async () => {
        setCameraStatus('pending');
        setCameraError(null);
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ 
                video: { facingMode: 'environment' } 
            });
            stream.getTracks().forEach(track => track.stop());
            setCameraStatus('granted');
        } catch (err) {
            console.error("Camera permission error:", err);
            if (err.name === 'NotAllowedError' || err.name === 'PermissionDeniedError') {
                setCameraStatus('denied');
                setCameraError('Camera access was denied. Please allow camera access in your browser settings.');
            } else {
                setCameraStatus('error');
                setCameraError(err.message || 'Failed to access camera');
            }
        }
    };

    return (
        <div className="min-h-screen bg-black flex flex-col items-center justify-center p-4 relative">
            <button
                onClick={() => navigate(-1)}
                className="absolute top-4 left-4 p-2 bg-white/10 rounded-full text-white backdrop-blur-sm z-10"
            >
                <ArrowLeft className="h-6 w-6" />
            </button>

            {/* CASE 1: Location Pending */}
            {permissionStatus === 'pending' && (
                <div className="text-white text-center">
                    <Loader2 className="h-10 w-10 animate-spin mx-auto mb-4 text-indigo-500" />
                    <h2 className="text-xl font-semibold">Requesting Location</h2>
                    <p className="text-gray-400 mt-2 mb-4">Please allow location access to mark attendance.</p>
                    <button
                        onClick={() => {
                            setCoords(null);
                            setPermissionStatus('granted');
                        }}
                        className="text-sm text-gray-500 hover:text-white underline"
                    >
                        Continue without location
                    </button>
                </div>
            )}

            {/* CASE 2: Location Denied/Unsupported */}
            {(permissionStatus === 'denied' || permissionStatus === 'unsupported') && (
                <div className="w-full max-w-md bg-white rounded-3xl p-8 flex flex-col items-center text-center shadow-xl">
                    <div className="h-20 w-20 bg-red-100 rounded-full flex items-center justify-center mb-6">
                        <MapPin className="h-10 w-10 text-red-600" />
                    </div>
                    <h2 className="text-2xl font-bold text-gray-900 mb-2">Location Required</h2>
                    <p className="text-gray-500 mb-6">
                        We need your location to verify you are in the classroom. Only continue if your instructor has disabled location checks.
                    </p>
                    <div className="flex flex-col gap-3 w-full">
                        <button
                            onClick={handleRetryLocation}
                            className="w-full py-3 bg-indigo-600 text-white rounded-xl font-medium hover:bg-indigo-700 transition"
                        >
                            Try Again
                        </button>
                        <button
                            onClick={() => {
                                setCoords(null);
                                setPermissionStatus('granted');
                            }}
                            className="w-full py-3 bg-gray-100 text-gray-700 rounded-xl font-medium hover:bg-gray-200 transition"
                        >
                            Continue without Location
                        </button>
                    </div>
                </div>
            )}

            {/* CASE 3: Location Granted - Check Camera Permission */}
            {permissionStatus === 'granted' && cameraStatus === 'pending' && (
                <div className="text-white text-center">
                    <Loader2 className="h-10 w-10 animate-spin mx-auto mb-4 text-indigo-500" />
                    <h2 className="text-xl font-semibold">Requesting Camera Access</h2>
                    <p className="text-gray-400 mt-2">Please allow camera access to scan QR codes.</p>
                </div>
            )}

            {/* CASE 4: Camera Denied/Error */}
            {permissionStatus === 'granted' && (cameraStatus === 'denied' || cameraStatus === 'error') && (
                <div className="w-full max-w-md bg-white rounded-3xl p-8 flex flex-col items-center text-center shadow-xl">
                    <div className="h-20 w-20 bg-red-100 rounded-full flex items-center justify-center mb-6">
                        <AlertCircle className="h-10 w-10 text-red-600" />
                    </div>
                    <h2 className="text-2xl font-bold text-gray-900 mb-2">Camera Access Required</h2>
                    <p className="text-gray-500 mb-4">
                        {cameraError || 'We need camera access to scan QR codes for attendance.'}
                    </p>
                    <p className="text-sm text-gray-400 mb-6">
                        On mobile: Check your browser settings or try opening in a different browser.
                    </p>
                    <button
                        onClick={handleRetryCamera}
                        className="w-full py-3 bg-indigo-600 text-white rounded-xl font-medium hover:bg-indigo-700 transition"
                    >
                        Try Again
                    </button>
                </div>
            )}

            {/* CASE 5: Location AND Camera Granted (Scanning or Result) */}
            {permissionStatus === 'granted' && cameraStatus === 'granted' && (
                <>
                    {scanResult ? (
                        <div className="w-full max-w-md bg-white rounded-3xl p-8 flex flex-col items-center text-center shadow-2xl animate-fade-in-up">
                            {scanResult.success ? (
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
                                    <p className="text-red-500 font-medium">{scanResult.message}</p>
                                    <button
                                        onClick={() => {
                                            setScanResult(null);
                                            setScanning(true);
                                        }}
                                        className="mt-8 w-full py-3 bg-indigo-600 text-white rounded-xl font-medium"
                                    >
                                        Try Again
                                    </button>
                                </>
                            )}
                        </div>
                    ) : (
                        <div className="w-full max-w-md bg-white rounded-3xl overflow-hidden shadow-2xl">
                            <div className="p-4 bg-gray-900 text-white text-center font-medium flex items-center justify-center gap-2">
                                <MapPin className="h-4 w-4 text-green-400" />
                                <span>Location Verified</span>
                            </div>
                            <div id="reader" className="w-full bg-black min-h-[300px]"></div>
                            <div className="p-6 text-center text-gray-500 text-sm">
                                Align the QR code within the frame.
                            </div>
                        </div>
                    )}
                </>
            )}
        </div>
    );
};
