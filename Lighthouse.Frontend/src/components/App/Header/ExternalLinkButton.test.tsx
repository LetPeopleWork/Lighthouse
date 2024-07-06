import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import ExternalLinkButton from './ExternalLinkButton';
import BugReportIcon from '@mui/icons-material/BugReport';
import { SvgIconComponent } from '@mui/icons-material';

describe('ExternalLinkButton component', () => {
  const renderComponent = (link: string, icon: SvgIconComponent) => 
    render(<ExternalLinkButton link={link} icon={icon} />);

  it('should render with the correct href', () => {
    const link = "https://github.com/LetPeopleWork/Lighthouse/issues";
    const { container } = renderComponent(link, BugReportIcon);
    const button = container.querySelector('a');
    expect(button).toHaveAttribute('href', link);
  });

  it('should open link in a new tab', () => {
    const link = "https://github.com/LetPeopleWork/Lighthouse/issues";
    const { container } = renderComponent(link, BugReportIcon);
    const button = container.querySelector('a');
    expect(button).toHaveAttribute('target', '_blank');
  });

  it('should have rel="noopener noreferrer"', () => {
    const link = "https://github.com/LetPeopleWork/Lighthouse/issues";
    const { container } = renderComponent(link, BugReportIcon);
    const button = container.querySelector('a');
    expect(button).toHaveAttribute('rel', 'noopener noreferrer');
  });

  it('should render the correct icon', () => {
    const link = "https://github.com/LetPeopleWork/Lighthouse/issues";
    const { container } = renderComponent(link, BugReportIcon);
    const icon = container.querySelector('svg');
    expect(icon).toBeTruthy();
  });

  it('should apply the correct color style to the icon', () => {
    const link = "https://github.com/LetPeopleWork/Lighthouse/issues";
    const { container } = renderComponent(link, BugReportIcon);
    const icon = container.querySelector('svg');
    expect(icon).toHaveStyle('color: rgba(48, 87, 78, 1)');
  });
});
