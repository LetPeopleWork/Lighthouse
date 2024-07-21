import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { IProjectSettings, ProjectSettings } from '../../../models/Project/ProjectSettings';
import AdvancedInputsComponent from './AdvancedInputs';

describe('AdvancedInputsComponent', () => {
    const initialSettings: IProjectSettings = new ProjectSettings(1, "Settings", [], [], "Initial Query", "Unparented Query", 10, 12)

    const mockOnProjectSettingsChange = vi.fn();

    it('renders correctly with initial settings', () => {
        render(
            <AdvancedInputsComponent
                projectSettings={initialSettings}
                onProjectSettingsChange={mockOnProjectSettingsChange}
            />
        );

        expect(screen.getByLabelText(/Unparented Work Items Query/i)).toHaveValue('Unparented Query');
        expect(screen.getByLabelText(/Default Number of Items per Feature/i)).toHaveValue(10);
    });

    it('calls onProjectSettingsChange with correct arguments when unparentedItemsQuery changes', () => {
        render(
            <AdvancedInputsComponent
                projectSettings={initialSettings}
                onProjectSettingsChange={mockOnProjectSettingsChange}
            />
        );

        fireEvent.change(screen.getByLabelText(/Unparented Work Items Query/i), { target: { value: 'Updated Query' } });

        expect(mockOnProjectSettingsChange).toHaveBeenCalledWith('unparentedItemsQuery', 'Updated Query');
    });

    it('calls onProjectSettingsChange with correct arguments when defaultAmountOfWorkItemsPerFeature changes', () => {
        render(
            <AdvancedInputsComponent
                projectSettings={initialSettings}
                onProjectSettingsChange={mockOnProjectSettingsChange}
            />
        );

        fireEvent.change(screen.getByLabelText(/Default Number of Items per Feature/i), { target: { value: '20' } });

        expect(mockOnProjectSettingsChange).toHaveBeenCalledWith('defaultAmountOfWorkItemsPerFeature', 20);
    });
});
