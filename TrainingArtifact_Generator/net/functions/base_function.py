class BaseFunction:
    def __init__(self, f, f_prime):
        self.f = f
        self.f_prime = f_prime

    def __call__(self, *args):
        return self.f(*args)

    def prime(self, *args):
        return self.f_prime(*args)
