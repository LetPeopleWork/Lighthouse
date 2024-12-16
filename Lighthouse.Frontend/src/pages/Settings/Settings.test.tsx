import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import Settings from './Settings';

// Mock the components used in the tabs
vi.mock('./Connections/WorkTrackingSystemConnectionSettings', () => ({
    default: () => <div>Work Tracking System Connection Settings</div>,
}));
vi.mock('./LogSettings/LogSettings', () => ({
    default: () => <div>Log Settings</div>,
}));
vi.mock('./Refresh/RefreshSettingsTab', () => ({
    default: () => <div>Refresh Settings</div>,
}));
vi.mock('./DefaultTeamSettings/DefaultTeamSettings', () => ({
    default: () => <div>Default Team Settings</div>,
}));
vi.mock('./DefaultProjectSettings/DefaultProjectSettings', () => ({
    default: () => <div>Default Project Settings</div>,
}));
vi.mock('./PreviewFeatures/PreviewFeaturesTab', () => ({
    default: () => <div>Preview Features</div>,
}));
vi.mock('../../components/App/LetPeopleWork/Tutorial/TutorialButton', () => ({
    default: () => <button>Tutorial Button</button>,
}));
vi.mock('../../components/App/LetPeopleWork/Tutorial/Tutorials/SettingsTutorial', () => ({
    default: () => <div>Settings Tutorial</div>,
}));

describe('Settings Component', () => {
    beforeEach(() => {
        render(<Settings />);
    });

    afterEach(() => {
        vi.resetAllMocks();
    });

    it('should render the initial tab correctly', () => {
        expect(screen.getByTestId('work-tracking-panel')).toBeVisible();
        expect(screen.queryByTestId('default-team-settings-panel')).not.toBeVisible();
    });

    it('should switch to Default Team Settings tab when clicked', () => {
        fireEvent.click(screen.getByTestId('default-team-settings-tab'));
        expect(screen.getByTestId('default-team-settings-panel')).toBeVisible();
        expect(screen.getByTestId('work-tracking-panel')).not.toBeVisible();
    });

    it('should switch to Default Project Settings tab when clicked', () => {
        fireEvent.click(screen.getByTestId('default-project-settings-tab'));
        expect(screen.getByTestId('default-project-settings-panel')).toBeVisible();
        expect(screen.getByTestId('work-tracking-panel')).not.toBeVisible();
    });

    it('should switch to Periodic Refresh Settings tab when clicked', () => {
        fireEvent.click(screen.getByTestId('periodic-refresh-settings-tab'));
        expect(screen.getByTestId('periodic-refresh-settings-panel')).toBeVisible();
        expect(screen.getByTestId('work-tracking-panel')).not.toBeVisible();
    });

    it('should switch to Data Retention Settings tab when clicked', () => {
        fireEvent.click(screen.getByTestId('data-retention-settings-tab'));
        expect(screen.getByTestId('data-retention-settings-panel')).toBeVisible();
        expect(screen.getByTestId('work-tracking-panel')).not.toBeVisible();
    });

    it('should switch to Preview Features tab when clicked', () => {
        fireEvent.click(screen.getByTestId('preview-features-tab'));
        expect(screen.getByTestId('preview-features-panel')).toBeVisible();
        expect(screen.getByTestId('work-tracking-panel')).not.toBeVisible();
    });

    it('should switch to Logs tab when clicked', () => {
        fireEvent.click(screen.getByTestId('logs-tab'));
        expect(screen.getByTestId('logs-panel')).toBeVisible();
        expect(screen.getByTestId('work-tracking-panel')).not.toBeVisible();
    });
});
