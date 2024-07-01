import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import React from 'react'

interface HeaderItemProps {
    link: string;
    icon: IconProp;
}

const HeaderItem: React.FC<HeaderItemProps> = ({ link, icon }) => {
    return (
        <div>
            <a href={link}>
                <FontAwesomeIcon icon={icon} />
            </a>
        </div>
    );
}

export default HeaderItem;