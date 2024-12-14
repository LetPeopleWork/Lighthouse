import { describe, it, expect, vi, afterEach } from 'vitest';
import { fireEvent, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import SampleFrequencySelector from './SampleFrequencySelector';

describe('SampleFrequencySelector component', () => {
    const onSampleEveryNthDayChange = vi.fn();

    afterEach(() => {
        vi.clearAllMocks();
    });

    it("should display label", async () => {
        render(<SampleFrequencySelector sampleEveryNthDay={7} onSampleEveryNthDayChange={onSampleEveryNthDayChange} />);

        expect(await screen.findByLabelText("Sampling Frequency")).toBeInTheDocument();
    });

    it("should display dropdown", async () => {
        render(<SampleFrequencySelector sampleEveryNthDay={7} onSampleEveryNthDayChange={onSampleEveryNthDayChange} />);

        expect(
            within(await screen.findByTestId("frequency-select")).getByRole("combobox"),
        ).toBeInTheDocument();
    });

    it("should display all options", async () => {
        render(<SampleFrequencySelector sampleEveryNthDay={7} onSampleEveryNthDayChange={onSampleEveryNthDayChange} />);

        const dropdown = within(await screen.findByTestId("frequency-select")).getByRole('combobox');
        await userEvent.click(dropdown);

        expect(screen.getByRole("option", { name: "Daily" })).toBeInTheDocument();
        expect(screen.getByRole("option", { name: "Weekly" })).toBeInTheDocument();
        expect(screen.getByRole("option", { name: "Monthly" })).toBeInTheDocument();
        expect(screen.getByRole("option", { name: "Custom" })).toBeInTheDocument();
    });

    const defaultDisplayFrequencyTestCases = [
        { sampleEveryNthDay: 1, expectedText: "Daily" },
        { sampleEveryNthDay: 7, expectedText: "Weekly" },
        { sampleEveryNthDay: 30, expectedText: "Monthly" },
    ];

    test.each(defaultDisplayFrequencyTestCases)(
        'should display the correct frequency for default $sampleEveryNthDay',
        async ({ sampleEveryNthDay, expectedText }) => {
            render(<SampleFrequencySelector sampleEveryNthDay={sampleEveryNthDay} onSampleEveryNthDayChange={onSampleEveryNthDayChange} />);
            expect(screen.getByText(expectedText)).toBeInTheDocument();
        }
    );

    test.each(defaultDisplayFrequencyTestCases)(
        'should switch to selected frequency for $sampleEveryNthDay',
        async ({ sampleEveryNthDay, expectedText }) => {
            render(<SampleFrequencySelector sampleEveryNthDay={7} onSampleEveryNthDayChange={onSampleEveryNthDayChange} />);
            const dropdown = within(await screen.findByTestId("frequency-select")).getByRole('combobox');
            await userEvent.click(dropdown);

            const dailyOption = screen.getByRole("option", { name: expectedText });
            await userEvent.click(dailyOption);

            expect(onSampleEveryNthDayChange).toHaveBeenCalledWith(sampleEveryNthDay);
        }
    );

    it('should display custom sampling frequency correct', async () => {
        render(<SampleFrequencySelector sampleEveryNthDay={42} onSampleEveryNthDayChange={onSampleEveryNthDayChange} />);

        expect(screen.getByText("Custom")).toBeInTheDocument();
        const customValue = screen.getByLabelText("Custom Sampling Interval (Days)");
        expect(customValue).toBeInTheDocument();
        expect(customValue).toHaveValue(42);
    });

    it('should switch to custom frequency', async () => {
        render(<SampleFrequencySelector sampleEveryNthDay={1} onSampleEveryNthDayChange={onSampleEveryNthDayChange} />);

        const dropdown = within(await screen.findByTestId("frequency-select")).getByRole('combobox');
        await userEvent.click(dropdown);

        const customOption = screen.getByRole("option", { name: "Custom" });
        await userEvent.click(customOption);        

        onSampleEveryNthDayChange.mockClear();

        expect(screen.getByText("Custom")).toBeInTheDocument();
        const customValue = screen.getByLabelText("Custom Sampling Interval (Days)");
        fireEvent.change(customValue, { target: { value: '7' } })

        expect(onSampleEveryNthDayChange).toHaveBeenCalledWith(7);
    });
});
