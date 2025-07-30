import { type ReactElement, useCallback, useEffect } from 'react';
import { toast } from '@tokens-studio/ui/Toast.js';

interface IToast {
  title: string;
  description: string | ReactElement;
  appearance?: ToastAppearance;
}

interface ErrorWithMessage {
  message: string;
}

interface ErrorWithErrors {
  errors: ErrorWithMessage[];
}

interface ErrorWithBody {
  body: {
    message: string;
  };
}

type ErrorType =
  | string
  | Error
  | ErrorWithErrors
  | ErrorWithBody
  | null
  | undefined;

export enum ToastAppearance {
  Error = 'error',
  Success = 'success',
  Message = 'message',
}

export const useToast = () => {
  const makeToast = useCallback((props: IToast) => {
    switch (props.appearance) {
      case ToastAppearance.Error:
        toast.error(props.title, {
          description: props.description,
        });
        break;
      case ToastAppearance.Success:
        toast.success(props.title, {
          description: props.description,
        });
        break;
      default:
        toast.message(props.title, {
          description: props.description,
        });
        break;
    }
  }, []);
  return makeToast;
};

export const useErrorToast = (error: ErrorType) => {
  const makeToast = useToast();
  useEffect(() => {
    if (error) {
      if (typeof error === 'string') {
        makeToast({
          title: 'Error',
          description: error,
          appearance: ToastAppearance.Error,
        });
      } else if (error instanceof Error) {
        makeToast({
          title: 'Error',
          description: error.message,
          appearance: ToastAppearance.Error,
        });
      } else if ('errors' in error && error.errors) {
        error.errors.forEach((element: ErrorWithMessage) => {
          makeToast({
            title: 'Error',
            description: element.message,
            appearance: ToastAppearance.Error,
          });
        });
      } else if ('body' in error && error.body && error.body.message) {
        makeToast({
          title: 'Error',
          description: error.body.message,
          appearance: ToastAppearance.Error,
        });
      }
    }
  }, [error, makeToast]);
};
