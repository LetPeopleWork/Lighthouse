import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { IProjectSettings, ProjectSettings } from '../../../models/Project/ProjectSettings';
import GeneralInputsComponent from './GeneralInputs';

describe('GeneralInputsComponent', () => {
    const initialSettings: IProjectSettings = new ProjectSettings(1, "Project Name", [], [], "Initial Query", "Unparented Query", false, 10, 85, "", 12, "")

    const mockOnProjectSettingsChange = vi.fn();

    it('renders correctly with initial settings', () => {
        render(
            <GeneralInputsComponent
                projectSettings={initialSettings}
                onProjectSettingsChange={mockOnProjectSettingsChange}
            />
        );

        expect(screen.getByLabelText(/Name/i)).toHaveValue('Project Name');
        expect(screen.getByLabelText(/Work Item Query/i)).toHaveValue('Initial Query');
    });

    it('calls onProjectSettingsChange with correct arguments when name changes', () => {
        render(
            <GeneralInputsComponent
                projectSettings={initialSettings}
                onProjectSettingsChange={mockOnProjectSettingsChange}
            />
        );

        fireEvent.change(screen.getByLabelText(/Name/i), { target: { value: 'New Project Name' } });

        expect(mockOnProjectSettingsChange).toHaveBeenCalledWith('name', 'New Project Name');
    });

    it('calls onProjectSettingsChange with correct arguments when workItemQuery changes', () => {
        render(
            <GeneralInputsComponent
                projectSettings={initialSettings}
                onProjectSettingsChange={mockOnProjectSettingsChange}
            />
        );

        fireEvent.change(screen.getByLabelText(/Work Item Query/i), { target: { value: 'Updated Query' } });

        expect(mockOnProjectSettingsChange).toHaveBeenCalledWith('workItemQuery', 'Updated Query');
    });

    it('handles null projectSettings correctly', () => {
        render(
            <GeneralInputsComponent
                projectSettings={null}
                onProjectSettingsChange={mockOnProjectSettingsChange}
            />
        );

        expect(screen.getByLabelText(/Name/i)).toHaveValue('');
        expect(screen.getByLabelText(/Work Item Query/i)).toHaveValue('');

        fireEvent.change(screen.getByLabelText(/Name/i), { target: { value: 'New Name' } });
        expect(mockOnProjectSettingsChange).toHaveBeenCalledWith('name', 'New Name');

        fireEvent.change(screen.getByLabelText(/Work Item Query/i), { target: { value: 'New Query' } });
        expect(mockOnProjectSettingsChange).toHaveBeenCalledWith('workItemQuery', 'New Query');
    });
});
