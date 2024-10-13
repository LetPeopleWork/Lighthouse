import { render, screen } from '@testing-library/react';
import { vi } from 'vitest';
import DatePickerComponent from './DatePickerComponent'; // Adjust the import according to your file structure
import dayjs from 'dayjs';
import userEvent from '@testing-library/user-event';

describe('DatePickerComponent', () => {
    const mockOnChange = vi.fn();

    afterEach(() => {
        vi.clearAllMocks();
    });

    test('renders the date picker with the correct label', () => {
        render(<DatePickerComponent label="Burndown Start Date" value={dayjs()} onChange={mockOnChange} />);

        expect(screen.getByLabelText(/burndown start date/i)).toBeInTheDocument();
    });

    test('calls onChange when a new date is selected', async () => {
        render(<DatePickerComponent label="Burndown Start Date" value={dayjs()} onChange={mockOnChange} />);

        const pastDate = dayjs(new Date(2024, 4, 8));
        await userEvent.type(screen.getByRole('textbox'), pastDate.toString())

        expect(mockOnChange).toHaveBeenCalled();
    });
});
