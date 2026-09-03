import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { ReleaseForm } from './ReleaseForm';

describe('ReleaseForm', () => {
  it('submits a draft with the typed entry', () => {
    const onSubmit = vi.fn();
    render(<ReleaseForm onSubmit={onSubmit} pending={false} />);

    fireEvent.change(screen.getByLabelText('Version'), { target: { value: '1.2.0' } });
    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Structured addresses' } });
    fireEvent.change(screen.getByLabelText('Component'), { target: { value: 'Validation' } });
    fireEvent.change(screen.getByLabelText('Entry title'), { target: { value: 'New rule set' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create draft' }));

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        version: '1.2.0',
        title: 'Structured addresses',
        entries: [expect.objectContaining({ component: 'Validation', title: 'New rule set', type: 'Feature' })],
      }),
    );
  });

  it('omits the entry when no entry title is given', () => {
    const onSubmit = vi.fn();
    render(<ReleaseForm onSubmit={onSubmit} pending={false} />);

    fireEvent.change(screen.getByLabelText('Version'), { target: { value: '1.2.1' } });
    fireEvent.change(screen.getByLabelText('Title'), { target: { value: 'Patch' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create draft' }));

    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ entries: [] }));
  });
});
