import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import React from 'react';

interface SidebarElementProps {
    text: string;
    link: string;
    icon: IconProp;
}

const SidebarElement: React.FC<SidebarElementProps> = ({ text, link, icon }) => {
    return (
        <div>
            <FontAwesomeIcon icon={icon} />
            <a href={link}>{text}</a>
        </div>)
};

export default SidebarElement;