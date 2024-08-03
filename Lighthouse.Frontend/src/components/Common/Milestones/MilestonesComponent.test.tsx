import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import MilestonesComponent from './MilestonesComponent';
import { IMilestone, Milestone } from '../../../models/Project/Milestone';

describe('MilestonesComponent', () => {
    const initialMilestones: IMilestone[] = [
        new Milestone(1, 'Milestone 1', new Date('2024-08-01')),
        new Milestone(2, 'Milestone 2', new Date('2024-09-01')),
    ];

    const mockAddMilestone = vi.fn();
    const mockRemoveMilestone = vi.fn();
    const mockUpdateMilestone = vi.fn();

    it('renders correctly with initial milestones', () => {
        render(
            <MilestonesComponent
                milestones={initialMilestones}
                onAddMilestone={mockAddMilestone}
                onRemoveMilestone={mockRemoveMilestone}
                onUpdateMilestone={mockUpdateMilestone}
            />
        );

        initialMilestones.forEach(milestone => {
            expect(screen.getByDisplayValue(milestone.name)).toBeInTheDocument();
            expect(screen.getByDisplayValue(milestone.date.toISOString().slice(0, 10))).toBeInTheDocument();
        });
    });

    it('adds a new milestone correctly', () => {
        render(
            <MilestonesComponent
                milestones={initialMilestones}
                onAddMilestone={mockAddMilestone}
                onRemoveMilestone={mockRemoveMilestone}
                onUpdateMilestone={mockUpdateMilestone}
            />
        );

        fireEvent.change(screen.getByLabelText(/New Milestone Name/i), { target: { value: 'New Milestone' } });
        fireEvent.change(screen.getByLabelText(/New Milestone Date/i), { target: { value: '2024-10-01' } });
        fireEvent.click(screen.getByText(/Add Milestone/i));

        expect(mockAddMilestone).toHaveBeenCalledWith(new Milestone(0, 'New Milestone', new Date('2024-10-01')));
    });

    it('updates a milestone correctly', async () => {
        render(
            <MilestonesComponent
                milestones={initialMilestones}
                onAddMilestone={mockAddMilestone}
                onRemoveMilestone={mockRemoveMilestone}
                onUpdateMilestone={mockUpdateMilestone}
            />
        );

        fireEvent.change(screen.getByDisplayValue('Milestone 1'), { target: { value: 'Updated Milestone 1' } });
        fireEvent.change(screen.getByDisplayValue('2024-08-01'), { target: { value: '2024-08-15' } });

        // We have to await this as we don't instantly update the name
        await new Promise(resolve => setTimeout(resolve, 1000))

        expect(mockUpdateMilestone).toHaveBeenCalledWith('Milestone 1', { name: 'Updated Milestone 1' });
        expect(mockUpdateMilestone).toHaveBeenCalledWith('Milestone 1', { date: new Date('2024-08-15') });
    });

    it('removes a milestone correctly', () => {
        render(
            <MilestonesComponent
                milestones={initialMilestones}
                onAddMilestone={mockAddMilestone}
                onRemoveMilestone={mockRemoveMilestone}
                onUpdateMilestone={mockUpdateMilestone}
            />
        );

        fireEvent.click(screen.getAllByRole('button', { name: /delete/i })[0]);

        expect(mockRemoveMilestone).toHaveBeenCalledWith('Milestone 1');
    });
});