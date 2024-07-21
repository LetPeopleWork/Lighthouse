import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import WorkItemTypesComponent from './WorkItemTypesComponent';

describe('WorkItemTypesComponent', () => {
    const mockOnAddWorkItemType = vi.fn();
    const mockOnRemoveWorkItemType = vi.fn();

    const workItemTypes = ['Bug', 'Feature', 'Task'];

    beforeEach(() => {
        mockOnAddWorkItemType.mockClear();
        mockOnRemoveWorkItemType.mockClear();
    });

    it('renders correctly', () => {
        render(
            <WorkItemTypesComponent
                workItemTypes={workItemTypes}
                onAddWorkItemType={mockOnAddWorkItemType}
                onRemoveWorkItemType={mockOnRemoveWorkItemType}
            />
        );

        expect(screen.getByText('Work Item Types')).toBeInTheDocument();

        workItemTypes.forEach(type => {
            expect(screen.getByText(type)).toBeInTheDocument();
        });

        expect(screen.getByLabelText('New Work Item Type')).toBeInTheDocument();
        expect(screen.getByText('Add Work Item Type')).toBeInTheDocument();
    });

    it('calls onAddWorkItemType when a new work item type is added', () => {
        render(
            <WorkItemTypesComponent
                workItemTypes={workItemTypes}
                onAddWorkItemType={mockOnAddWorkItemType}
                onRemoveWorkItemType={mockOnRemoveWorkItemType}
            />
        );

        const input = screen.getByLabelText('New Work Item Type');
        const addButton = screen.getByText('Add Work Item Type');

        fireEvent.change(input, { target: { value: 'Improvement' } });
        fireEvent.click(addButton);

        expect(mockOnAddWorkItemType).toHaveBeenCalledWith('Improvement');
        expect(mockOnAddWorkItemType).toHaveBeenCalledTimes(1);

        expect(input).toHaveValue('');
    });

    it('does not call onAddWorkItemType when the input is empty', () => {
        render(
            <WorkItemTypesComponent
                workItemTypes={workItemTypes}
                onAddWorkItemType={mockOnAddWorkItemType}
                onRemoveWorkItemType={mockOnRemoveWorkItemType}
            />
        );

        const addButton = screen.getByText('Add Work Item Type');

        fireEvent.click(addButton);

        expect(mockOnAddWorkItemType).not.toHaveBeenCalled();
    });


    it('calls onRemoveWorkItemType when a work item type is removed', () => {
        render(
            <WorkItemTypesComponent
                workItemTypes={workItemTypes}
                onAddWorkItemType={mockOnAddWorkItemType}
                onRemoveWorkItemType={mockOnRemoveWorkItemType}
            />
        );

        const deleteButton = screen.getAllByRole('button', { name: 'delete' })[0];
        fireEvent.click(deleteButton);

        expect(mockOnRemoveWorkItemType).toHaveBeenCalledWith(workItemTypes[0]);
        expect(mockOnRemoveWorkItemType).toHaveBeenCalledTimes(1);
    });
});
