import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ItemListManager from './ItemListManager';

describe('ItemListManager', () => {
    const mockOnAddItem = vi.fn();
    const mockOnRemoveItem = vi.fn();
    const title = 'Item';
    const items = ['Item 1', 'Item 2'];

    beforeEach(() => {
        mockOnAddItem.mockClear();
        mockOnRemoveItem.mockClear();
    });

    it('renders the initial items correctly', () => {
        render(<ItemListManager title={title} items={items} onAddItem={mockOnAddItem} onRemoveItem={mockOnRemoveItem} />);
        items.forEach(item => {
            expect(screen.getByText(item)).toBeInTheDocument();
        });
    });

    it('calls onAddItem when a new item is added', () => {
        render(<ItemListManager title={title} items={items} onAddItem={mockOnAddItem} onRemoveItem={mockOnRemoveItem} />);
        
        const input = screen.getByLabelText(`New ${title}`);
        const addButton = screen.getByRole('button', { name: `Add ${title}` });

        fireEvent.change(input, { target: { value: 'Item 3' } });
        fireEvent.click(addButton);

        expect(mockOnAddItem).toHaveBeenCalledWith('Item 3');
        expect(input).toHaveValue('');
    });

    it('does not call onAddItem when the input is empty', () => {
        render(<ItemListManager title={title} items={items} onAddItem={mockOnAddItem} onRemoveItem={mockOnRemoveItem} />);
        
        const addButton = screen.getByRole('button', { name: `Add ${title}` });
        fireEvent.click(addButton);

        expect(mockOnAddItem).not.toHaveBeenCalled();
    });

    it('calls onRemoveItem when an item is removed', () => {
        render(<ItemListManager title={title} items={items} onAddItem={mockOnAddItem} onRemoveItem={mockOnRemoveItem} />);
        
        const deleteButtons = screen.getAllByLabelText('delete');
        fireEvent.click(deleteButtons[0]);

        expect(mockOnRemoveItem).toHaveBeenCalledWith('Item 1');
    });
});
