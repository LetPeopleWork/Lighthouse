import React from 'react';

interface LocalDateTimeDisplayProps {
    utcDate: Date | string;
    showTime?: boolean;
}

const LocalDateTimeDisplay: React.FC<LocalDateTimeDisplayProps> = ({ utcDate, showTime = false }) => {
    const dateObject = (utcDate instanceof Date) ? utcDate : new Date(utcDate)
    const localDateTimeString = showTime ? dateObject.toLocaleString() : dateObject.toLocaleDateString();

    if (isNaN(dateObject.getTime())) {
        return <div>Invalid Date</div>;
    }

    return (
        <>
            {localDateTimeString}
        </>
    );
};

export default LocalDateTimeDisplay;
