import { createContext, useContext, useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from './AuthContext';

const SignalRContext = createContext(null);

export const SignalRProvider = ({ children }) => {
    const { user } = useAuth();
    const [connection, setConnection] = useState(null);
    const [isConnected, setIsConnected] = useState(false);

    const defaultApiOrigin = `${window.location.protocol}//${window.location.hostname}:5038`;
    const apiOrigin = import.meta.env.VITE_API_ORIGIN || defaultApiOrigin;

    useEffect(() => {
        if (user?.userType === 'Instructor' && !connection) {
            const newConnection = new signalR.HubConnectionBuilder()
                .withUrl(`${apiOrigin}/hubs/session`, {
                    accessTokenFactory: () => localStorage.getItem('token') || '',
                })
                .withAutomaticReconnect()
                .build();

            setConnection(newConnection);
        }
    }, [user, connection]);

    useEffect(() => {
        if (connection) {
            connection
                .start()
                .then(() => {
                    console.log('SignalR Connected');
                    setIsConnected(true);
                })
                .catch((err) => console.error('SignalR Connection Error: ', err));

            connection.onclose(() => {
                setIsConnected(false);
            });

            return () => {
                connection.stop();
            };
        }
    }, [connection]);

    return (
        <SignalRContext.Provider value={{ connection, isConnected }}>
            {children}
        </SignalRContext.Provider>
    );
};

export const useSignalR = () => useContext(SignalRContext);
