import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';
import LogSettings from './LogSettings';
import { ILogService } from '../../../services/Api/LogService';
import { createMockApiServiceContext, createMockLogService } from '../../../tests/MockApiServiceProvider';

const mockGetLogs = vi.fn();
const mockGetLogLevel = vi.fn();
const mockGetSupportedLogLevels = vi.fn();
const mockSetLogLevel = vi.fn();

const mockLogService: ILogService = createMockLogService();
mockLogService.getLogs = mockGetLogs;
mockLogService.getLogLevel = mockGetLogLevel;
mockLogService.getSupportedLogLevels = mockGetSupportedLogLevels;
mockLogService.setLogLevel = mockSetLogLevel;

const MockApiServiceProvider = ({ children }: { children: React.ReactNode }) => {
    const mockContext = createMockApiServiceContext({ logService: mockLogService });

    return (
        <ApiServiceContext.Provider value={mockContext} >
            {children}
        </ApiServiceContext.Provider>
    );
};

describe('LogSettings', () => {
    let originalCreateObjectURL: typeof URL.createObjectURL;
    let originalAppendChild: typeof document.body.appendChild;
    let originalRemoveChild: typeof document.body.removeChild;

    beforeEach(() => {
        originalCreateObjectURL = URL.createObjectURL;
        originalAppendChild = document.body.appendChild;
        originalRemoveChild = document.body.removeChild;

        mockGetLogs.mockResolvedValue('Sample log data');
        mockGetLogLevel.mockResolvedValue('info');
        mockGetSupportedLogLevels.mockResolvedValue(['info', 'warn', 'error']);
        mockSetLogLevel.mockResolvedValue(undefined);
    });

    afterEach(() => {
        URL.createObjectURL = originalCreateObjectURL;
        document.body.appendChild = originalAppendChild;
        document.body.removeChild = originalRemoveChild;

        vi.resetAllMocks();
        vi.restoreAllMocks();
    });

    it('renders correctly and loads data', async () => {
        render(
            <MockApiServiceProvider>
                <LogSettings />
            </MockApiServiceProvider>
        );

        await waitFor(() => {
            expect(mockGetLogs).toHaveBeenCalled();
            expect(mockGetLogLevel).toHaveBeenCalled();
            expect(mockGetSupportedLogLevels).toHaveBeenCalled();
        });

        expect(screen.getByText('Sample log data')).toBeInTheDocument();
    });

    it('updates log level when changed', async () => {
        render(
            <MockApiServiceProvider>
                <LogSettings />
            </MockApiServiceProvider>
        );
    
        // Wait for initial data to load
        await waitFor(() => {
            expect(mockGetLogs).toHaveBeenCalled();
            expect(mockGetLogLevel).toHaveBeenCalled();
            expect(mockGetSupportedLogLevels).toHaveBeenCalled();
        });
    
        // Fire event to change the select value
        const select = screen.getByTestId("select-id");
        fireEvent.change(select, { target: { value: 'warn' } });
    
        // Wait for the mockSetLogLevel to be called
        await waitFor(() => {
            expect(mockSetLogLevel).toHaveBeenCalledWith('warn');
        });
    });    

    it('refreshes logs when refresh button is clicked', () => {
        render(
            <MockApiServiceProvider>
                <LogSettings />
            </MockApiServiceProvider>
        );

        const refreshButton = screen.getByRole('button', { name: /Refresh/i });

        fireEvent.click(refreshButton);

        expect(mockGetLogs).toHaveBeenCalled();
    });

    it('downloads logs when download button is clicked', async () => {
        render(
            <MockApiServiceProvider>
                <LogSettings />
            </MockApiServiceProvider>
        );

        const downloadButton = screen.getByRole('button', { name: /Download/i });

        fireEvent.click(downloadButton);

        await waitFor(() => {
            expect(mockGetLogs).toHaveBeenCalled();
        });
    });
});