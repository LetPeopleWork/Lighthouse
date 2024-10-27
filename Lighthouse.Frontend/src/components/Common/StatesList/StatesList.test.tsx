import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import StatesList from './StatesList';

describe('StatesList', () => {
    const mockOnAddToDoState = vi.fn();
    const mockOnRemoveToDoState = vi.fn();
    const mockOnAddDoingState = vi.fn();
    const mockOnRemoveDoingState = vi.fn();
    const mockOnAddDoneState = vi.fn();
    const mockOnRemoveDoneState = vi.fn();

    const toDoStates = ['Task 1', 'Task 2'];
    const doingStates = ['In Progress'];
    const doneStates = ['Completed'];

    beforeEach(() => {
        mockOnAddToDoState.mockClear();
        mockOnRemoveToDoState.mockClear();
        mockOnAddDoingState.mockClear();
        mockOnRemoveDoingState.mockClear();
        mockOnAddDoneState.mockClear();
        mockOnRemoveDoneState.mockClear();
    });

    it('renders the title correctly', () => {
        render(<StatesList
            toDoStates={toDoStates}
            onAddToDoState={mockOnAddToDoState}
            onRemoveToDoState={mockOnRemoveToDoState}
            doingStates={doingStates}
            onAddDoingState={mockOnAddDoingState}
            onRemoveDoingState={mockOnRemoveDoingState}
            doneStates={doneStates}
            onAddDoneState={mockOnAddDoneState}
            onRemoveDoneState={mockOnRemoveDoneState}
        />);
        
        expect(screen.getByText('States')).toBeInTheDocument();
    });

    it('renders to do states correctly', () => {
        render(<StatesList
            toDoStates={toDoStates}
            onAddToDoState={mockOnAddToDoState}
            onRemoveToDoState={mockOnRemoveToDoState}
            doingStates={doingStates}
            onAddDoingState={mockOnAddDoingState}
            onRemoveDoingState={mockOnRemoveDoingState}
            doneStates={doneStates}
            onAddDoneState={mockOnAddDoneState}
            onRemoveDoneState={mockOnRemoveDoneState}
        />);

        toDoStates.forEach(state => {
            expect(screen.getByText(state)).toBeInTheDocument();
        });
    });

    it('calls onAddToDoState when a new to do state is added', () => {
        render(<StatesList
            toDoStates={toDoStates}
            onAddToDoState={mockOnAddToDoState}
            onRemoveToDoState={mockOnRemoveToDoState}
            doingStates={doingStates}
            onAddDoingState={mockOnAddDoingState}
            onRemoveDoingState={mockOnRemoveDoingState}
            doneStates={doneStates}
            onAddDoneState={mockOnAddDoneState}
            onRemoveDoneState={mockOnRemoveDoneState}
        />);

        const input = screen.getByLabelText('New To Do States');
        const addButton = screen.getByRole('button', { name: 'Add To Do States' });

        fireEvent.change(input, { target: { value: 'Task 3' } });
        fireEvent.click(addButton);

        expect(mockOnAddToDoState).toHaveBeenCalledWith('Task 3');
    });

    it('calls onRemoveToDoState when an item is removed', () => {
        render(<StatesList
            toDoStates={toDoStates}
            onAddToDoState={mockOnAddToDoState}
            onRemoveToDoState={mockOnRemoveToDoState}
            doingStates={doingStates}
            onAddDoingState={mockOnAddDoingState}
            onRemoveDoingState={mockOnRemoveDoingState}
            doneStates={doneStates}
            onAddDoneState={mockOnAddDoneState}
            onRemoveDoneState={mockOnRemoveDoneState}
        />);

        const deleteButtons = screen.getAllByLabelText('delete');
        fireEvent.click(deleteButtons[0]);

        expect(mockOnRemoveToDoState).toHaveBeenCalledWith('Task 1');
    });

    it('renders doing states correctly', () => {
        render(<StatesList
            toDoStates={toDoStates}
            onAddToDoState={mockOnAddToDoState}
            onRemoveToDoState={mockOnRemoveToDoState}
            doingStates={doingStates}
            onAddDoingState={mockOnAddDoingState}
            onRemoveDoingState={mockOnRemoveDoingState}
            doneStates={doneStates}
            onAddDoneState={mockOnAddDoneState}
            onRemoveDoneState={mockOnRemoveDoneState}
        />);

        doingStates.forEach(state => {
            expect(screen.getByText(state)).toBeInTheDocument();
        });
    });

    it('calls onAddDoingState when a new doing state is added', () => {
        render(<StatesList
            toDoStates={toDoStates}
            onAddToDoState={mockOnAddToDoState}
            onRemoveToDoState={mockOnRemoveToDoState}
            doingStates={doingStates}
            onAddDoingState={mockOnAddDoingState}
            onRemoveDoingState={mockOnRemoveDoingState}
            doneStates={doneStates}
            onAddDoneState={mockOnAddDoneState}
            onRemoveDoneState={mockOnRemoveDoneState}
        />);

        const input = screen.getByLabelText('New Doing States');
        const addButton = screen.getByRole('button', { name: 'Add Doing States' });

        fireEvent.change(input, { target: { value: 'Task 4' } });
        fireEvent.click(addButton);

        expect(mockOnAddDoingState).toHaveBeenCalledWith('Task 4');
    });

    it('calls onRemoveDoingState when an item is removed', () => {
        render(<StatesList
            toDoStates={toDoStates}
            onAddToDoState={mockOnAddToDoState}
            onRemoveToDoState={mockOnRemoveToDoState}
            doingStates={doingStates}
            onAddDoingState={mockOnAddDoingState}
            onRemoveDoingState={mockOnRemoveDoingState}
            doneStates={doneStates}
            onAddDoneState={mockOnAddDoneState}
            onRemoveDoneState={mockOnRemoveDoneState}
        />);

        const deleteButtons = screen.getAllByLabelText('delete');
        fireEvent.click(deleteButtons[2]);

        expect(mockOnRemoveDoingState).toHaveBeenCalledWith('In Progress');
    });

    it('renders done states correctly', () => {
        render(<StatesList
            toDoStates={toDoStates}
            onAddToDoState={mockOnAddToDoState}
            onRemoveToDoState={mockOnRemoveToDoState}
            doingStates={doingStates}
            onAddDoingState={mockOnAddDoingState}
            onRemoveDoingState={mockOnRemoveDoingState}
            doneStates={doneStates}
            onAddDoneState={mockOnAddDoneState}
            onRemoveDoneState={mockOnRemoveDoneState}
        />);

        doneStates.forEach(state => {
            expect(screen.getByText(state)).toBeInTheDocument();
        });
    });

    it('calls onAddDoneState when a new done state is added', () => {
        render(<StatesList
            toDoStates={toDoStates}
            onAddToDoState={mockOnAddToDoState}
            onRemoveToDoState={mockOnRemoveToDoState}
            doingStates={doingStates}
            onAddDoingState={mockOnAddDoingState}
            onRemoveDoingState={mockOnRemoveDoingState}
            doneStates={doneStates}
            onAddDoneState={mockOnAddDoneState}
            onRemoveDoneState={mockOnRemoveDoneState}
        />);

        const input = screen.getByLabelText('New Done States');
        const addButton = screen.getByRole('button', { name: 'Add Done States' });

        fireEvent.change(input, { target: { value: 'Task 5' } });
        fireEvent.click(addButton);

        expect(mockOnAddDoneState).toHaveBeenCalledWith('Task 5');
    });

    it('calls onRemoveDoneState when an item is removed', () => {
        render(<StatesList
            toDoStates={toDoStates}
            onAddToDoState={mockOnAddToDoState}
            onRemoveToDoState={mockOnRemoveToDoState}
            doingStates={doingStates}
            onAddDoingState={mockOnAddDoingState}
            onRemoveDoingState={mockOnRemoveDoingState}
            doneStates={doneStates}
            onAddDoneState={mockOnAddDoneState}
            onRemoveDoneState={mockOnRemoveDoneState}
        />);

        const deleteButtons = screen.getAllByLabelText('delete');
        fireEvent.click(deleteButtons[3]);

        expect(mockOnRemoveDoneState).toHaveBeenCalledWith('Completed');
    });
});
