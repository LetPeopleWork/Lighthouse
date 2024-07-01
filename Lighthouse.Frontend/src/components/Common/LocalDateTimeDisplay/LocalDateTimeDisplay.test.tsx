import { render, screen } from '@testing-library/react';
import { describe, test, expect, vi } from 'vitest';
import LocalDateTimeDisplay from './LocalDateTimeDisplay';

describe('LocalDateTimeDisplay', () => {
  const testCases = [
    { utcDate: new Date('2024-06-24T12:00:00Z'), showTime: true, expected: '6/24/2024, 12:00:00 PM' },
    { utcDate: new Date('2024-06-24T12:00:00Z'), showTime: false, expected: '6/24/2024' },
    { utcDate: '2024-06-24T12:00:00Z', showTime: true, expected: '6/24/2024, 12:00:00 PM' },
    { utcDate: '2024-06-24T12:00:00Z', showTime: false, expected: '6/24/2024' },
  ];

  const mockToLocaleString = vi.fn(() => '6/24/2024, 12:00:00 PM');
  const mockToLocaleDateString = vi.fn(() => '6/24/2024')
  
  let originalToLocaleString: (this: Date, locales?: string | string[], options?: Intl.DateTimeFormatOptions) => string;
  let originalToLocaleDateString: (this: Date, locales?: string | string[], options?: Intl.DateTimeFormatOptions) => string;
  
  beforeEach(() => {    
    originalToLocaleString = Date.prototype.toLocaleString;
    originalToLocaleDateString = Date.prototype.toLocaleDateString;

    Date.prototype.toLocaleString = mockToLocaleString;
    Date.prototype.toLocaleDateString = mockToLocaleDateString;
  });

  afterEach(() => {
    Date.prototype.toLocaleString = originalToLocaleString;
    Date.prototype.toLocaleDateString = originalToLocaleDateString;
  })

  test.each(testCases)(
    'renders the correct local date and time string',
    ({ utcDate, showTime, expected }) => {
      render(<LocalDateTimeDisplay utcDate={utcDate} showTime={showTime} />);
      const dateTimeElement = screen.getByText(expected);
      expect(dateTimeElement).toBeInTheDocument();
    }
  );

  test('handles invalid date string gracefully', () => {
    render(<LocalDateTimeDisplay utcDate="invalid-date-string" />);
    const dateTimeElement = screen.getByText('Invalid Date');
    expect(dateTimeElement).toBeInTheDocument();
  });
});
