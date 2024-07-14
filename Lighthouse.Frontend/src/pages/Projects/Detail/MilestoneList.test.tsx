import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import MilestoneList from './MilestoneList';
import { IMilestone, Milestone } from '../../../models/Milestone';
import dayjs from 'dayjs';

describe('MilestoneList component', () => {
    const milestones: IMilestone[] = [
        new Milestone(1, "Milestone 1", new Date('2023-07-01')),
        new Milestone(2, "Milestone 2", new Date('2023-08-01')),
    ];

    it('should render without errors when there are milestones', () => {
        render(<MilestoneList milestones={milestones} />);

        expect(screen.getByText('Milestones')).toBeInTheDocument();

        milestones.forEach((milestone) => {
            const datePickerLabel = `${milestone.name} Target Date`;
            const datePicker = screen.getByLabelText(datePickerLabel);
            expect(datePicker).toBeInTheDocument();
            expect(datePicker).toHaveAttribute('disabled');
            expect(datePicker).toHaveValue(dayjs(milestone.date).format('MM/DD/YYYY'));
        });
    });

    it('should render nothing when there are no milestones', () => {
        render(<MilestoneList milestones={[]} />);

        expect(screen.queryByText('Milestones')).not.toBeInTheDocument();
    });
});
