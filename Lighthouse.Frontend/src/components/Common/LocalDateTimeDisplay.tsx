import React from 'react';

interface LocalDateTimeDisplayProps {
    utcDate: Date;
    showTime?: boolean;
}

const LocalDateTimeDisplay: React.FC<LocalDateTimeDisplayProps> = ({ utcDate, showTime = false }) => {
    const dateObject = (utcDate instanceof Date) ? utcDate : new Date(utcDate)
    const localDateTimeString = showTime ? dateObject.toLocaleString() : dateObject.toLocaleDateString();

    return (
        <div>
            {localDateTimeString}
        </div>
    );
};

export default LocalDateTimeDisplay;
