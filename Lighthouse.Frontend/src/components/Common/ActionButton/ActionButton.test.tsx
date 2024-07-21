import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import ActionButton from './ActionButton';

describe('ActionButton', () => {
    it('renders correctly', () => {
        render(<ActionButton buttonText="Click Me" isWaiting={false} onClickHandler={vi.fn()} />);
        expect(screen.getByText('Click Me')).toBeInTheDocument();
    });

    it('shows spinner when isWaiting is true', () => {
        render(<ActionButton buttonText="Click Me" isWaiting={true} onClickHandler={vi.fn()} />);
        expect(screen.getByRole('progressbar')).toBeInTheDocument();
    });

    it('does not show spinner when isWaiting is false', () => {
        render(<ActionButton buttonText="Click Me" isWaiting={false} onClickHandler={vi.fn()} />);
        expect(screen.queryByRole('progressbar')).toBeNull();
    });

    it('calls onClickHandler when button is clicked', () => {
        const onClickHandler = vi.fn();
        render(<ActionButton buttonText="Click Me" isWaiting={false} onClickHandler={onClickHandler} />);
        fireEvent.click(screen.getByText('Click Me'));
        expect(onClickHandler).toHaveBeenCalled();
    });

    it('disables the button when isWaiting is true', () => {
        render(<ActionButton buttonText="Click Me" isWaiting={true} onClickHandler={vi.fn()} />);
        expect(screen.getByText('Click Me')).toBeDisabled();
    });

    it('does not disable the button when isWaiting is false', () => {
        render(<ActionButton buttonText="Click Me" isWaiting={false} onClickHandler={vi.fn()} />);
        expect(screen.getByText('Click Me')).not.toBeDisabled();
    });

    it('displays the correct button text', () => {
        render(<ActionButton buttonText="Submit" isWaiting={false} onClickHandler={vi.fn()} />);
        expect(screen.getByText('Submit')).toBeInTheDocument();
    });
});
