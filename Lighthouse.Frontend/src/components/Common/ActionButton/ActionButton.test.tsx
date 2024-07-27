import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ActionButton from './ActionButton';

describe('ActionButton', () => {
    it('renders correctly', () => {
        render(<ActionButton buttonText="Click Me" onClickHandler={vi.fn()} />);
        expect(screen.getByText('Click Me')).toBeInTheDocument();
    });

    it('shows spinner when executing action', () => {
        const longRunningAction = () => new Promise<void>(() => {});

        render(<ActionButton buttonText="Click Me" onClickHandler={longRunningAction} />);
        fireEvent.click(screen.getByText('Click Me'));

        expect(screen.getByRole('progressbar')).toBeInTheDocument();
    });

    it('does not show spinner when action is not running', () => {
        render(<ActionButton buttonText="Click Me" onClickHandler={vi.fn()} />);
        expect(screen.queryByRole('progressbar')).toBeNull();
    });

    it('calls onClickHandler when button is clicked', () => {
        const onClickHandler = vi.fn();
        render(<ActionButton buttonText="Click Me" onClickHandler={onClickHandler} />);
        fireEvent.click(screen.getByText('Click Me'));
        expect(onClickHandler).toHaveBeenCalled();
    });

    it('disables the button while action is running', () => {
        const longRunningAction = () => new Promise<void>(() => {});

        render(<ActionButton buttonText="Click Me" onClickHandler={longRunningAction} />);
        fireEvent.click(screen.getByText('Click Me'));

        expect(screen.getByText('Click Me')).toBeDisabled();
    });

    it('does not disable the button when action is not running', () => {
        render(<ActionButton buttonText="Click Me" onClickHandler={vi.fn()} />);
        expect(screen.getByText('Click Me')).not.toBeDisabled();
    });

    it('displays the correct button text', () => {
        render(<ActionButton buttonText="Submit" onClickHandler={vi.fn()} />);
        expect(screen.getByText('Submit')).toBeInTheDocument();
    });
});
