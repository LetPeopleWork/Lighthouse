import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { IProjectSettings, ProjectSettings } from '../../../models/Project/ProjectSettings';
import AdvancedInputsComponent from './AdvancedInputs';

describe('AdvancedInputsComponent', () => {
    const initialSettings: IProjectSettings = new ProjectSettings(
        1,
        "Settings",
        [],
        [],
        "Initial Query",
        "Unparented Query",
        false,
        10,
        85,
        "Historical Feature Query",
        12,
        ""
    );

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
        expect(screen.getByLabelText(/Size Estimate Field/i)).toHaveValue("");
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

    it('calls onProjectSettingsChange with correct arguments when sizeEstimateField changes', () => {
        render(
            <AdvancedInputsComponent
                projectSettings={initialSettings}
                onProjectSettingsChange={mockOnProjectSettingsChange}
            />
        );

        fireEvent.change(screen.getByLabelText(/Size Estimate Field/i), { target: { value: 'customfield_133742' } });

        expect(mockOnProjectSettingsChange).toHaveBeenCalledWith('sizeEstimateField', 'customfield_133742');
    });

    it('toggles the usePercentileToCalculateDefaultAmountOfWorkItems switch', () => {
        render(
            <AdvancedInputsComponent
                projectSettings={initialSettings}
                onProjectSettingsChange={mockOnProjectSettingsChange}
            />
        );

        const toggleSwitch = screen.getByLabelText(/Use Percentile to Calculate Default Amount of Work Items/i);

        expect(toggleSwitch).not.toBeChecked();
        fireEvent.click(toggleSwitch);
        expect(mockOnProjectSettingsChange).toHaveBeenCalledWith('usePercentileToCalculateDefaultAmountOfWorkItems', true);
    });

    it('renders Default Work Item Percentile and Historical Features Work Item Query when switch is on', () => {
        const updatedSettings: IProjectSettings = {
            ...initialSettings,
            usePercentileToCalculateDefaultAmountOfWorkItems: true,
        };

        render(
            <AdvancedInputsComponent
                projectSettings={updatedSettings}
                onProjectSettingsChange={mockOnProjectSettingsChange}
            />
        );

        expect(screen.getByLabelText(/Default Work Item Percentile/i)).toBeInTheDocument();
        expect(screen.getByLabelText(/Historical Features Work Item Query/i)).toBeInTheDocument();
    });

    it('calls onProjectSettingsChange with correct arguments when defaultWorkItemPercentile changes', () => {
        const updatedSettings: IProjectSettings = {
            ...initialSettings,
            usePercentileToCalculateDefaultAmountOfWorkItems: true,
        };

        render(
            <AdvancedInputsComponent
                projectSettings={updatedSettings}
                onProjectSettingsChange={mockOnProjectSettingsChange}
            />
        );

        fireEvent.change(screen.getByLabelText(/Default Work Item Percentile/i), { target: { value: '90' } });

        expect(mockOnProjectSettingsChange).toHaveBeenCalledWith('defaultWorkItemPercentile', 90);
    });
});
