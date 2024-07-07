import React from 'react';
import { useParams } from 'react-router-dom';

const EditProject: React.FC = () => {
    const { id } = useParams<{ id?: string }>();

    const isNewProject: boolean = id === undefined;

    return (
        <div>
            {isNewProject ? (
                <h1>New Project</h1>
            ) : (
                <h1>Edit</h1>
            )}
            <p>Selected Project ID: {id}</p>
        </div>
    );
}

export default EditProject;
