import React from 'react';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import dayjs, { Dayjs } from 'dayjs';

interface DatePickerComponentProps {
    label: string;
    value: Dayjs;
    onChange: (newValue: Dayjs | null) => void;
}

const DatePickerComponent: React.FC<DatePickerComponentProps> = ({ label, value, onChange }) => {
    const handleChange = (newValue: Dayjs | null) => {
        if (!newValue || newValue.isAfter(dayjs())) {
            return;
        }

        onChange(newValue);
    };


    return (
        <LocalizationProvider dateAdapter={AdapterDayjs}>
            <DatePicker
                label={label}
                value={value}
                onChange={handleChange}
                maxDate={dayjs()}
                sx={{ width: '100%' }}
            />
        </LocalizationProvider>
    );
};

export default DatePickerComponent;
