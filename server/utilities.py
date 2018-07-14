import re


def safe_string(string):
    return re.sub(r'\W+', '', string)
