import React from 'react';
import { useParams } from 'react-router-dom';

const TeamDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();

    return (
        <div>
            <h1>Team Detail</h1>
            <p>Selected Team ID: {id}</p>
        </div>
    );
}

export default TeamDetail;
