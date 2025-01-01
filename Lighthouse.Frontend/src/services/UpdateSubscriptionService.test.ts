import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { UpdateSubscriptionService, IUpdateStatus } from './UpdateSubscriptionService';
import * as signalR from '@microsoft/signalr';

vi.mock('@microsoft/signalr');

describe('UpdateSubscriptionService', () => {
    let service: UpdateSubscriptionService;
    let mockConnection: signalR.HubConnection;

    beforeEach(() => {
        mockConnection = {
            start: vi.fn().mockResolvedValue(undefined),
            on: vi.fn(),
            off: vi.fn(),
            invoke: vi.fn(),
            stop: vi.fn(),
        } as unknown as signalR.HubConnection;

        (signalR.HubConnectionBuilder.prototype.withUrl as import('@vitest/spy').Mock).mockReturnValue({
            configureLogging: vi.fn().mockReturnValue({
                build: vi.fn().mockReturnValue(mockConnection),
            }),
        });

        service = new UpdateSubscriptionService();
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it('should initialize the connection', async () => {
        await service.initialize();
        expect(mockConnection.start).toHaveBeenCalled();
    });

    it('should subscribe to team updates', async () => {
        const callback = vi.fn();
        await service.subscribeToTeamUpdates(1, callback);
        expect(mockConnection.on).toHaveBeenCalledWith('Team_1', callback);
        expect(mockConnection.invoke).toHaveBeenCalledWith('SubscribeToUpdate', 'Team', 1);
    });

    it('should unsubscribe from team updates', async () => {
        await service.unsubscribeFromTeamUpdates(1);
        expect(mockConnection.off).toHaveBeenCalledWith('Team_1');
        expect(mockConnection.invoke).toHaveBeenCalledWith('UnsubscribeFromUpdate', 'Team', 1);
    });

    it('should get update status', async () => {
        await service.initialize();
        const mockStatus: IUpdateStatus = { updateType: 'Team', id: 1, status: 'Completed' };
        (mockConnection.invoke as import('@vitest/spy').Mock).mockResolvedValue(mockStatus);

        const status = await service.getUpdateStatus('Team', 1);
        expect(status).toEqual(mockStatus);
        expect(mockConnection.invoke).toHaveBeenCalledWith('GetUpdateStatus', 'Team', 1);
    });

    it('should handle errors when getting update status', async () => {
        await service.initialize();
        (mockConnection.invoke as import('@vitest/spy').Mock).mockRejectedValue(new Error('Test error'));

        const status = await service.getUpdateStatus('Team', 1);
        expect(status).toBeNull();
        expect(mockConnection.invoke).toHaveBeenCalledWith('GetUpdateStatus', 'Team', 1);
    });
});
