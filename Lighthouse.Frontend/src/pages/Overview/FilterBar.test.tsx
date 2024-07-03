import { render, screen, fireEvent } from '@testing-library/react';
import { describe, expect } from 'vitest';
import FilterBar from './FilterBar';

describe('FilterBar', () => {
  test('renders the input with the correct placeholder and value', () => {
    const filterText = "test";
    const onFilterTextChange = vi.fn();

    render(<FilterBar filterText={filterText} onFilterTextChange={onFilterTextChange} />);

    const inputElement = screen.getByPlaceholderText('Search...');
    expect(inputElement).toBeInTheDocument();
    expect(inputElement).toHaveValue(filterText);
  });

  test('calls onFilterTextChange when the input value changes', () => {
    const filterText = "test";
    const onFilterTextChange = vi.fn();

    render(<FilterBar filterText={filterText} onFilterTextChange={onFilterTextChange} />);

    const inputElement = screen.getByPlaceholderText('Search...');
    fireEvent.change(inputElement, { target: { value: 'new text' } });

    // Check if onFilterTextChange is called with the correct value
    expect(onFilterTextChange).toHaveBeenCalledTimes(1);
    expect(onFilterTextChange).toHaveBeenCalledWith('new text');
  });
});