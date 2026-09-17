import { initialsOf } from './initials';

/**
 * An avatar is a recognition cue, so the same name has to produce the same letters everywhere. These
 * cases are the ones the three copies of this function used to disagree about.
 */
describe('initialsOf', () => {
  it('takes the first letter of the first two words', () => {
    expect(initialsOf('Ada Lovelace')).toBe('AL');
  });

  it('uses a single letter for a one-word name', () => {
    expect(initialsOf('Prince')).toBe('P');
  });

  it('ignores extra words beyond the second', () => {
    expect(initialsOf('Ada King Lovelace')).toBe('AK');
  });

  it('survives repeated and leading whitespace', () => {
    expect(initialsOf('  ada   lovelace ')).toBe('AL');
  });

  it('returns nothing for an empty name rather than throwing', () => {
    expect(initialsOf('   ')).toBe('');
  });
});
