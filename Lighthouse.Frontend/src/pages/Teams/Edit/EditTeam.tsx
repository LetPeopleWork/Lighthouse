import React from 'react';
import { useParams } from 'react-router-dom';

const EditTeam: React.FC = () => {
    const { id } = useParams<{ id?: string }>();

    const isNewTeam: boolean = id === undefined;

    return (
        <div>
            {isNewTeam ? (
                <h1>New Team</h1>
            ) : (
                <h1>Edit</h1>
            )}
            <p>Selected Team ID: {id}</p>
        </div>
    );
}

export default EditTeam;
