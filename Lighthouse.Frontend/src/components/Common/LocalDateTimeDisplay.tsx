import React from 'react';

interface LocalDateTimeDisplayProps {
    utcDate: Date;
    showTime?: boolean;
}

const LocalDateTimeDisplay: React.FC<LocalDateTimeDisplayProps> = ({ utcDate, showTime = false }) => {
    const localDateTimeString = showTime ? utcDate.toLocaleString() : utcDate.toLocaleDateString();

    return (
        <div>
            {localDateTimeString}
        </div>
    );
};

export default LocalDateTimeDisplay;
