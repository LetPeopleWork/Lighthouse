import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import NavigationItem from './NavigationItem';

describe('NavigationItem component', () => {
    it('should render with the correct text', () => {
        const { getByText } = render(
            <MemoryRouter>
                <NavigationItem path="/home" text="Home" />
            </MemoryRouter>
        );
        expect(getByText('Home')).toBeTruthy();
    });

    it('should have the correct link path', () => {
        const { getByText } = render(
            <MemoryRouter>
                <NavigationItem path="/home" text="Home" />
            </MemoryRouter>
        );
        const linkElement = getByText('Home').closest('a');
        expect(linkElement).toHaveAttribute('href', '/home');
    });

    it('should have "nav-link" class by default', () => {
        const { getByText } = render(
            <MemoryRouter>
                <NavigationItem path="/home" text="Home" />
            </MemoryRouter>
        );
        const linkElement = getByText('Home').closest('a');
        expect(linkElement).toHaveClass('nav-link');
    });

    it('should have "nav-link active" class when active', () => {
        const { getByText } = render(
            <MemoryRouter initialEntries={['/home']}>
                <NavigationItem path="/home" text="Home" />
            </MemoryRouter>
        );
        const linkElement = getByText('Home').closest('a');
        expect(linkElement).toHaveClass('nav-link active');
    });

    it('should not have "active" class when not active', () => {
        const { getByText } = render(
            <MemoryRouter initialEntries={['/']}>
                <NavigationItem path="/home" text="Home" />
            </MemoryRouter>
        );
        const linkElement = getByText('Home').closest('a');
        expect(linkElement).not.toHaveClass('active');
    });
});
