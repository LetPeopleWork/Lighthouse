import React from 'react';
import { useParams } from 'react-router-dom';

const ProjectDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();

    return (
        <div>
            <h1>Project Detail</h1>
            <p>Selected Project ID: {id}</p>
        </div>
    );
}

export default ProjectDetail;
