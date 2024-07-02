import { render, screen } from '@testing-library/react';
import { describe, test, expect, vi } from 'vitest';
import LoadingAnimation from './LoadingAnimation';

vi.mock('react-spinners', () => {
    return {
        SyncLoader: () => <div data-testid="sync-loader">Loading...</div>
    };
});

describe('LoadingAnimation', () => {
    test('displays loading indicator when isLoading is true', async () => {
        render(<LoadingAnimation isLoading={true} hasError={false} children={<div></div>} />);

        expect(screen.getByTestId('sync-loader')).toBeInTheDocument();
    });

    test('displays error message when hasError is true', async () => {
        render(<LoadingAnimation isLoading={false} hasError={true} children={<div></div>} />);

        expect(screen.getByText('Error loading data. Please try again later.')).toBeInTheDocument();
    });

    test('displays children when not loading and no error', async () => {
        render(<LoadingAnimation isLoading={false} hasError={false} children={<div>Content</div>} />);

        expect(screen.getByText('Content')).toBeInTheDocument();
    });
});