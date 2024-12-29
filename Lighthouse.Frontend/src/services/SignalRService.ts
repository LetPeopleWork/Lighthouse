import * as signalR from '@microsoft/signalr';

class SignalRService {
    private static instance: SignalRService;
    private readonly connection: signalR.HubConnection;

    private constructor() {
        let baseUrl = "/api";
        if (import.meta.env.VITE_API_BASE_URL !== undefined) {
            baseUrl = import.meta.env.VITE_API_BASE_URL;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${baseUrl}/updateNotificationHub`, {
                withCredentials: true
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.connection.start().catch(err => console.error('Error starting SignalR connection:', err)).then(() => this.subscribeToTeamUpdates(1, () => console.log("UPDATE!")));
    }

    public static getInstance(): SignalRService {
        if (!SignalRService.instance) {
            SignalRService.instance = new SignalRService();
        }
        return SignalRService.instance;
    }

    subscribeToUpdate(updateType: string, id: number, callback: (status: string) => void) {
        const updateKey = `${updateType}_${id}`;
        this.connection.on(updateKey, callback);
        this.connection.invoke('SubscribeToUpdate', updateType, id).catch(err => console.error('Error subscribing to update:', err));
    }

    unsubscribeFromUpdate(updateType: string, id: number) {
        const updateKey = `${updateType}_${id}`;
        this.connection.off(updateKey);
        this.connection.invoke('UnsubscribeFromUpdate', updateType, id).catch(err => console.error('Error unsubscribing from update:', err));
    }

    subscribeToTeamUpdates(teamId: number, callback: (status: string) => void) {
        this.subscribeToUpdate('Team', teamId, callback);
    }

    unsubscribeFromTeamUpdates(teamId: number) {
        this.unsubscribeFromUpdate('Team', teamId);
    }
}

export default SignalRService.getInstance();
