import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import PreviewFeaturesTab from './PreviewFeaturesTab';
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';
import { createMockApiServiceContext, createMockPreviewFeatureService } from '../../../tests/MockApiServiceProvider';
import { IPreviewFeatureService } from '../../../services/Api/PreviewFeatureService';

// Mocking the Loading Animation component
vi.mock('../../../components/Common/LoadingAnimation/LoadingAnimation', () => ({
    default: ({ hasError, isLoading, children }: { hasError: boolean, isLoading: boolean, children: React.ReactNode }) => (
        <>
            {isLoading && <div>Loading...</div>}
            {hasError && <div>Error loading data</div>}
            {!isLoading && !hasError && children}
        </>
    ),
}));

const mockPreviewFeatureService: IPreviewFeatureService = createMockPreviewFeatureService();

const mockGetAllFeatures = vi.fn();
const mockUpdateFeature = vi.fn();

mockPreviewFeatureService.getAllFeatures = mockGetAllFeatures;
mockPreviewFeatureService.updateFeature = mockUpdateFeature;

const MockApiServiceProvider = ({ children }: { children: React.ReactNode }) => {
    const mockContext = createMockApiServiceContext({ previewFeatureService: mockPreviewFeatureService });

    return (
        <ApiServiceContext.Provider value={mockContext}>
            {children}
        </ApiServiceContext.Provider>
    );
};

const renderWithMockApiProvider = () => {
    render(
        <MockApiServiceProvider>
            <PreviewFeaturesTab />
        </MockApiServiceProvider>
    );
};

describe('PreviewFeaturesTab component', () => {
    beforeEach(() => {
        mockGetAllFeatures.mockResolvedValue([
            { id: 1, name: 'Feature 1', key: 'feature1', description: 'Description 1', enabled: false },
            { id: 2, name: 'Feature 2', key: 'feature2', description: 'Description 2', enabled: true },
        ]);
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it('should fetch and display preview features', async () => {
        renderWithMockApiProvider();

        await waitFor(() => {
            expect(screen.getByText('Feature 1')).toBeVisible();
        });
        
        const switches = screen.getAllByRole('checkbox');
        expect(switches[0]).not.toBeChecked();
        expect(switches[1]).toBeChecked();
    });

    it('should toggle the enabled state of a feature', async () => {
        renderWithMockApiProvider();

        // Wait for the features to load
        await waitFor(() => {
            expect(screen.getByText('Feature 1')).toBeVisible();
        });

        const switchElement = screen.getAllByRole('checkbox')[0];
        fireEvent.click(switchElement);

        expect(mockUpdateFeature).toHaveBeenCalledWith({
            id: 1,
            name: 'Feature 1',
            key: 'feature1',
            description: 'Description 1',
            enabled: true,
        });

        // Wait for the state to update and check if the switch reflects the new state
        await waitFor(() => {
            expect(switchElement).toBeChecked();
        });
    });
});