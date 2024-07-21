import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { ITeamSettings, TeamSettings } from '../../../models/Team/TeamSettings';
import GeneralInputsComponent from './GeneralInputs';

// Mock InputGroup component
vi.mock('../../../components/Common/InputGroup/InputGroup', () => ({
    __esModule: true,
    default: ({ title, children }: { title: string, children: React.ReactNode }) => (
        <div>
            <h2>{title}</h2>
            {children}
        </div>
    ),
}));

describe('GeneralInputsComponent', () => {
    const onTeamSettingsChange = vi.fn();

    const teamSettings: ITeamSettings = new TeamSettings(
        0,
        'Test Name',
        10,
        20,
        'Test Query',
        [],
        12,
        'Test Field'
    );

    it('renders correctly with provided teamSettings', () => {
        render(
            <GeneralInputsComponent
                teamSettings={teamSettings}
                onTeamSettingsChange={onTeamSettingsChange}
            />
        );

        // Check if the TextFields display the correct values
        expect(screen.getByLabelText('Name')).toHaveValue('Test Name');
        expect(screen.getByLabelText('Throughput History')).toHaveValue(10);
        expect(screen.getByLabelText('Feature WIP')).toHaveValue(20);
        expect(screen.getByLabelText('Work Item Query')).toHaveValue('Test Query');
    });

    it('calls onTeamSettingsChange with correct parameters when the Name TextField value changes', () => {
        render(
            <GeneralInputsComponent
                teamSettings={teamSettings}
                onTeamSettingsChange={onTeamSettingsChange}
            />
        );

        fireEvent.change(screen.getByLabelText('Name'), {
            target: { value: 'New Name' }
        });

        expect(onTeamSettingsChange).toHaveBeenCalledWith('name', 'New Name');
    });

    it('calls onTeamSettingsChange with correct parameters when Throughput History TextField value changes', () => {
        render(
            <GeneralInputsComponent
                teamSettings={teamSettings}
                onTeamSettingsChange={onTeamSettingsChange}
            />
        );

        fireEvent.change(screen.getByLabelText('Throughput History'), {
            target: { value: '15' }
        });

        expect(onTeamSettingsChange).toHaveBeenCalledWith('throughputHistory', 15);
    });

    it('calls onTeamSettingsChange with correct parameters when Feature WIP TextField value changes', () => {
        render(
            <GeneralInputsComponent
                teamSettings={teamSettings}
                onTeamSettingsChange={onTeamSettingsChange}
            />
        );

        fireEvent.change(screen.getByLabelText('Feature WIP'), {
            target: { value: '25' }
        });

        expect(onTeamSettingsChange).toHaveBeenCalledWith('featureWIP', 25);
    });

    it('calls onTeamSettingsChange with correct parameters when Work Item Query TextField value changes', () => {
        render(
            <GeneralInputsComponent
                teamSettings={teamSettings}
                onTeamSettingsChange={onTeamSettingsChange}
            />
        );

        fireEvent.change(screen.getByLabelText('Work Item Query'), {
            target: { value: 'New Query' }
        });

        expect(onTeamSettingsChange).toHaveBeenCalledWith('workItemQuery', 'New Query');
    });
});