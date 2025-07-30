import { GraphToolbar } from './index.js';
import { Provider } from 'react-redux';
import { ReactFlowProvider } from 'reactflow';
import { TooltipContainer } from '@tokens-studio/ui';
import { store } from '@/redux/store.js';
import React from 'react';

describe('<PlayControls />', () => {
  it('renders', () => {
    cy.mount(
      <>
        <Provider store={store}>
          <ReactFlowProvider>
            <GraphToolbar />
          </ReactFlowProvider>
        </Provider>
        <TooltipContainer id="default-tooltip-id" />
      </>,
    );
  });
});
