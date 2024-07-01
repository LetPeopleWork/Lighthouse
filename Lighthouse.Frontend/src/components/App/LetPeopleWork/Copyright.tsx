import React from 'react'

const Copyright : React.FC = () => {
    const currentYear = new Date().getFullYear();
    
    return (
        <div>
            © {currentYear} - Let People Work
        </div>
    );
}

export default Copyright;