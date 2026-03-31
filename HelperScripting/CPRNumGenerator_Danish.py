"""
CPRNumGenerator_Danish.py
--------------------------
Generates valid Danish CPR numbers that pass the Da validation rules:
  1. First 6 digits must form a valid date (DDMMYY)
  2. Must contain exactly 10 digits
  3. 7th digit determines the century
  4. If born after 2006 -> valid (skips modulus 11)
  5. If born 2006 or earlier -> must pass modulus 11 checksum
"""

import random
from datetime import date, timedelta


# ---------------------------------------------------------------------------
# Century resolution (mirrors the X++ switch block)
# ---------------------------------------------------------------------------
def resolve_century(seventh: int, year_2digit: int) -> int:
    if seventh in (0, 1, 2, 3):
        return 1900
    elif seventh in (4, 9):
        return 1900 if year_2digit >= 37 else 2000
    elif seventh in (5, 6, 7, 8):
        return 1800 if year_2digit >= 58 else 2000
    raise ValueError(f"Invalid seventh digit: {seventh}")


# ---------------------------------------------------------------------------
# Modulus 11 check (mirrors X++ modulo11 logic for Danish CPR)
# Weights: 4 3 2 7 6 5 4 3 2 1
# ---------------------------------------------------------------------------
def modulo11(cpr: str) -> bool:
    weights = [4, 3, 2, 7, 6, 5, 4, 3, 2, 1]
    digits = [int(c) for c in cpr if c.isdigit()]
    if len(digits) != 10:
        return False
    total = sum(d * w for d, w in zip(digits, weights))
    return total % 11 == 0


# ---------------------------------------------------------------------------
# Full Danish CPR validator (mirrors checkCPRNumber Da branch)
# Returns:
#   True  -> valid (passes all checks)
#   False -> invalid
#   2     -> soft pass (modulus failed but _modulusCheck=False)
# ---------------------------------------------------------------------------
def validate_danish_cpr(cpr: str, modulus_check: bool = True):
    digits_only = ''.join(c for c in cpr if c.isdigit())

    # Must have exactly 10 digits
    if len(digits_only) != 10:
        return False

    dd   = int(digits_only[0:2])
    mm   = int(digits_only[2:4])
    yy   = int(digits_only[4:6])
    seventh = int(digits_only[6])

    # Validate date
    century  = resolve_century(seventh, yy)
    full_year = century + yy
    try:
        birth_date = date(full_year, mm, dd)
    except ValueError:
        return False

    # Born after 2006 -> skip modulus 11
    if full_year > 2006:
        return True

    # Otherwise apply modulus 11
    if modulo11(digits_only):
        return True

    if not modulus_check:
        return 2  # soft pass

    return False


# ---------------------------------------------------------------------------
# Generate a random valid birth date between start and end
# ---------------------------------------------------------------------------
def random_birth_date(start: date, end: date) -> date:
    delta = (end - start).days
    return start + timedelta(days=random.randint(0, delta))


# ---------------------------------------------------------------------------
# Determine valid 7th digit choices for a given birth year
# ---------------------------------------------------------------------------
def valid_seventh_digits(full_year: int) -> list:
    yy = full_year % 100
    valid = []
    for d in range(10):
        try:
            century = resolve_century(d, yy)
            if century + yy == full_year:
                valid.append(d)
        except ValueError:
            pass
    return valid


# ---------------------------------------------------------------------------
# Generate a single valid Danish CPR number
# ---------------------------------------------------------------------------
def generate_danish_cpr(
    post_2006: bool = True,
    modulus_check: bool = True,
    max_attempts: int = 10_000
) -> str:
    """
    Parameters
    ----------
    post_2006     : If True, generates a birth date after 2006 (skips modulus 11).
    modulus_check : If False, a number that only soft-passes modulus 11 is accepted.
    max_attempts  : Safety cap on generation loop.

    Returns
    -------
    A 10-digit string that passes Danish CPR validation.
    """
    if post_2006:
        start = date(2007, 1, 1)
        end   = date(2025, 12, 31)
    else:
        start = date(1900, 1, 1)
        end   = date(2006, 12, 31)

    for _ in range(max_attempts):
        bdate   = random_birth_date(start, end)
        dd      = f"{bdate.day:02d}"
        mm      = f"{bdate.month:02d}"
        yy      = f"{bdate.year % 100:02d}"
        full_year = bdate.year

        sevenths = valid_seventh_digits(full_year)
        if not sevenths:
            continue
        seventh = random.choice(sevenths)

        # Random last 3 digits (positions 8-10)
        last3 = f"{random.randint(0, 999):03d}"

        cpr = dd + mm + yy + str(seventh) + last3

        result = validate_danish_cpr(cpr, modulus_check)
        if result is True or (not modulus_check and result == 2):
            return cpr

    raise RuntimeError("Could not generate a valid CPR number within max_attempts.")


# ---------------------------------------------------------------------------
# Bulk generator
# ---------------------------------------------------------------------------
def generate_multiple(count: int = 5, post_2006: bool = True, modulus_check: bool = True) -> list:
    return [generate_danish_cpr(post_2006, modulus_check) for _ in range(count)]


# ---------------------------------------------------------------------------
# Pretty-print helper  ->  DDMMYY-XXXX
# ---------------------------------------------------------------------------
def format_cpr(cpr: str) -> str:
    return f"{cpr[:6]}-{cpr[6:]}"


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
if __name__ == "__main__":
    print("=" * 50)
    print("  Danish CPR Number Generator")
    print("=" * 50)

    print("\n[1] Post-2006 CPR numbers (skips modulus 11):")
    for cpr in generate_multiple(count=5, post_2006=True):
        print(f"  Raw: {cpr}   Formatted: {format_cpr(cpr)}   Valid: {validate_danish_cpr(cpr)}")

    print("\n[2] Pre-2006 CPR numbers (must pass modulus 11):")
    for cpr in generate_multiple(count=5, post_2006=False, modulus_check=True):
        print(f"  Raw: {cpr}   Formatted: {format_cpr(cpr)}   Valid: {validate_danish_cpr(cpr)}")

    print("\n[3] Single custom generation:")
    cpr = generate_danish_cpr(post_2006=False, modulus_check=False)
    print(f"  Raw: {cpr}   Formatted: {format_cpr(cpr)}   Valid: {validate_danish_cpr(cpr, modulus_check=False)}")