import { render, screen, fireEvent } from '@testing-library/react';
import { describe, test, expect, vi } from 'vitest';
import DeleteConfirmationDialog from './DeleteConfirmationDialog';

describe('DeleteConfirmationDialog', () => {
    test('displays the confirmation dialog with the correct item name', () => {
        render(<DeleteConfirmationDialog open={true} itemName="Test Item" onClose={vi.fn()} />);

        expect(screen.getByText('Confirm Delete')).toBeInTheDocument();
        expect(screen.getByText('Do you really want to delete Test Item?')).toBeInTheDocument();
        expect(screen.getByText('Cancel')).toBeInTheDocument();
        expect(screen.getByText('Delete')).toBeInTheDocument();
    });

    test('calls onClose with false when cancel button is clicked', () => {
        const onClose = vi.fn();
        render(<DeleteConfirmationDialog open={true} itemName="Test Item" onClose={onClose} />);

        fireEvent.click(screen.getByText('Cancel'));
        expect(onClose).toHaveBeenCalledWith(false);
    });

    test('calls onClose with true when delete button is clicked', () => {
        const onClose = vi.fn();
        render(<DeleteConfirmationDialog open={true} itemName="Test Item" onClose={onClose} />);

        fireEvent.click(screen.getByText('Delete'));
        expect(onClose).toHaveBeenCalledWith(true);
    });
});
